export const resolveLocalityId = (input, cachedLocalities) => {
    const match = cachedLocalities.find(l => l.localityId === input || l.localityName === input)
    return match?.localityId || input
  }
  
  export const rebuildSuggestions = (list, targetElement) => {
    targetElement.innerHTML = ''
    list
      .slice()
      .sort((a, b) => a.localityName.localeCompare(b.localityName))
      .forEach(({ localityName }) => {
        const option = document.createElement('option')
        option.value = localityName
        targetElement.appendChild(option)
      })
  }
  
  export const updateVisibility = (endpointSelect, resourceIdInput) => {
    resourceIdInput.classList.toggle('hidden', !endpointSelect.value.includes('{id}'))
  }
  
  export const buildDropdown = (endpoint, json, dropdown) => {
    if (!Array.isArray(json)) return
  
    const names = json
      .map(entry => {
        if (endpoint === '/v1/localities') return entry.localityName
        if (endpoint.includes('/streets')) return entry.streetName
        return entry.localityName
      })
      .filter(Boolean)
      .sort((a, b) => a.localeCompare(b))
  
    dropdown.innerHTML = ''
    names.forEach(name => {
      const option = document.createElement('option')
      option.value = name
      option.textContent = name
      dropdown.appendChild(option)
    })
  }