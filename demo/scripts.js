import {
  resolveLocalityId,
  rebuildSuggestions,
  updateVisibility,
  buildDropdown
} from './funcs/scripts.helpers.js'

import {
  runBenchmark,
  summarizeBenchmark
} from './funcs/scripts.benchmark.js'

import {
  updateComparisonHighlights,
  renderLatencyChart
} from './funcs/scripts.chart.js'

import {
  initializeMap,
  showOnMap,
  showStreetOnMap
} from './funcs/scripts.map.js'

const containers = [
  { id: 'monolith', label: 'Monolith', baseUrl: '/monolith' },
  { id: 'microservice', label: 'Microservices', baseUrl: '/microservice' }
]

const containerRefs = {}
const summaries = {}

const templateHtml = await fetch('./funcs/container.template.html').then(r => r.text())

containers.forEach(async ({ id, label, baseUrl }) => {
  const container = document.getElementById(id)
  containerRefs[id] = container

  container.innerHTML = templateHtml
    .replaceAll('{label}', label)
    .replaceAll('{datalist-id}', `${id}-suggestions`)
  

  // Element references
  const $ = selector => container.querySelector(selector)
  const endpointSelect = $('.endpoint')
  const correlationInput = $('.correlationId')
  const refreshIdBtn = $('.refreshIdBtn')
    correlationInput.value = crypto.randomUUID()
    refreshIdBtn.addEventListener('click', () => {
      correlationInput.value = crypto.randomUUID()
    })

  const resourceIdInput = $('.resourceId')
  const submitBtn = $('.submitBtn')
  const runBtn = $('.runBenchmark')
  const exportJsonBtn = $('.exportJson')
  const benchmarkOutput = $('.benchmarkOutput')
  const output = $('.output')
  const headersOutput = $('.headersOutput')
  const virtualUsersInput = $('.virtualUsers')
  const durationInput = $('.duration')
  const dropdown = $('.resultDropdown')
  const suggestionsList = $(`#${id}-suggestions`)

  // State
  let cachedLocalities = []
  let latestResults = []
  let latestSummary = null

  dropdown.addEventListener('change', async () => {
    if (!endpointSelect.value.includes('/streets')) return
    const streetName = dropdown.value
    const localityId = resolveLocalityId(resourceIdInput.value.trim(), cachedLocalities)
    const locality = cachedLocalities.find(l => l.localityId === localityId)
    const localityName = locality?.localityName
    if (!streetName || !localityName) return
    await showStreetOnMap(container, streetName, localityName)
  })

  // Handlers
  submitBtn.addEventListener('click', async () => {
    let endpoint = endpointSelect.value
    const correlationId = correlationInput.value.trim()
    const input = resourceIdInput.value.trim()
    if (!correlationId) return alert('Enter correlation ID')
    if (endpoint.includes('{id}') && !input) return alert('Enter ID')

    const resolvedId = resolveLocalityId(input, cachedLocalities)
    endpoint = endpoint.replace('{id}', encodeURIComponent(resolvedId))
    const fullUrl = `${baseUrl}${endpoint}`

    try {
      const res = await fetch(fullUrl, { headers: { 'X-Correlation-ID': correlationId } })
      const text = await res.text()
      
      // Show response headers

      /* // this is full header
      const headers = [...res.headers.entries()]
        .map(([key, value]) => `${key}: ${value}`)
        .join('\n')
      headersOutput.textContent = `Status: ${res.status}\n${headers}`
      */

      // this is trimmed header
      const etag = res.headers.get('etag') || 'N/A'
      const correlation = res.headers.get('x-correlation-id') || 'N/A'
      
      headersOutput.textContent = [
        `Status: ${res.status}`,
        `etag: ${etag}`,
        `x-correlation-id: ${correlation}`
      ].join('\n')


      // Show response body
      output.textContent = text      

      try {
        const json = JSON.parse(text)
        buildDropdown(endpoint, json, dropdown)

        const shouldShowDropdown =
          endpoint === '/v1/localities' || endpoint.includes('/streets')
        
        dropdown.classList.toggle('hidden', !shouldShowDropdown)        

        if (endpoint === '/v1/localities') {
          cachedLocalities = json
          rebuildSuggestions(cachedLocalities, suggestionsList)
          dropdown.addEventListener('change', () => {
            const selectedName = dropdown.value
            if (selectedName) showOnMap(container, selectedName)
          }, { once: true })
        }

        if (endpoint.startsWith('/v1/localities/') && !endpoint.includes('/streets')) {
          const loc = json
          if (loc?.localityName) showOnMap(container, loc.localityName)
        }
      } catch {}
    } catch (err) {
      output.textContent = `ERROR: ${err.message}`
    }
  })

  runBtn.addEventListener('click', async () => {
    let endpoint = endpointSelect.value
    const correlationId = correlationInput.value.trim()
    const input = resourceIdInput.value.trim()
    if (!correlationId) return alert('Enter correlation ID')
    if (endpoint.includes('{id}') && !input) return alert('Enter ID')

    const resolvedId = resolveLocalityId(input, cachedLocalities)
    endpoint = endpoint.replace('{id}', encodeURIComponent(resolvedId))
    const fullUrl = `${baseUrl}${endpoint}`

    const virtualUsers = parseInt(virtualUsersInput.value)
    const duration = parseInt(durationInput.value)

    benchmarkOutput.textContent = 'Running benchmark...'
    const results = await runBenchmark(fullUrl, correlationId, virtualUsers, duration)
    latestResults = results
    latestSummary = summarizeBenchmark(results, duration)
    latestSummary.timestamp = new Date().toISOString()
    benchmarkOutput.textContent = JSON.stringify(latestSummary, null, 2)

    summaries[id] = latestSummary
    updateComparisonHighlights(summaries, containerRefs)
    renderLatencyChart(summaries)
  })

  exportJsonBtn.addEventListener('click', () => {
    const blob = new Blob([JSON.stringify({ summary: latestSummary, results: latestResults }, null, 2)], {
      type: 'application/json'
    })
    const a = document.createElement('a')
    a.href = URL.createObjectURL(blob)
    a.download = `${label.toLowerCase()}-benchmark-${new Date().toISOString().replace(/[:.]/g, '-')}.json`
    a.click()
  })

  endpointSelect.addEventListener('change', () => updateVisibility(endpointSelect, resourceIdInput))
  updateVisibility(endpointSelect, resourceIdInput)
  dropdown.classList.add('hidden')
  dropdown.innerHTML = ''

  // Preload suggestions
  fetch(`${baseUrl}/v1/localities`, {
    headers: { 'X-Correlation-ID': 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee' }
  })
    .then(res => res.json())
    .then(json => {
      if (Array.isArray(json)) {
        cachedLocalities = json
        rebuildSuggestions(cachedLocalities, suggestionsList)
      }
    })
    .catch(err => console.warn(`${label} preload error:`, err.message))
})

document.getElementById('runBothBtn').addEventListener('click', () => {
  document.querySelectorAll('.runBenchmark').forEach(btn => btn.click())
})
