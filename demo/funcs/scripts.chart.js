export const updateComparisonHighlights = (summaries, containerRefs) => {
    const [a, b] = Object.keys(summaries)
    if (!a || !b) return
  
    const rpsA = parseFloat(summaries[a].rps)
    const rpsB = parseFloat(summaries[b].rps)
  
    const win = rpsA > rpsB ? a : b
    const lose = rpsA > rpsB ? b : a
  
    containerRefs[win].classList.add('winner')
    containerRefs[win].classList.remove('loser')
  
    containerRefs[lose].classList.add('loser')
    containerRefs[lose].classList.remove('winner')
  }
  
  let latencyChart
  
  export const renderLatencyChart = (summaries) => {
    const wrapper = document.getElementById('latency-chart-wrapper')
    if (wrapper.classList.contains('hidden')) wrapper.classList.remove('hidden')
    const [a, b] = Object.keys(summaries)
    if (!a || !b) return
  
    const sa = summaries[a]
    const sb = summaries[b]
  
    const ctxId = 'latencyChartCanvas'
    let canvas = document.getElementById(ctxId)
    if (!canvas) {
      canvas = document.createElement('canvas')
      canvas.id = ctxId
      document.getElementById('latency-chart').innerHTML = ''
      document.getElementById('latency-chart').appendChild(canvas)
    }
  
    const data = {
      labels: ['min_ms', 'max_ms', 'avg_ms', 'p50', 'p90', 'p99', 'rps'],
      datasets: [
        {
          label: 'Monolith',
          data: [sa.min_ms, sa.max_ms, sa.avg_ms, sa.p50, sa.p90, sa.p99, sa.rps],
          backgroundColor: 'rgba(34, 197, 94, 0.7)'
        },
        {
          label: 'Microservices',
          data: [sb.min_ms, sb.max_ms, sb.avg_ms, sb.p50, sb.p90, sb.p99, sb.rps],
          backgroundColor: 'rgba(239, 68, 68, 0.7)'
        }
      ]
    }
  
    const config = {
      type: 'bar',
      data,
      options: {
        responsive: true,
        plugins: {
          title: { display: true, text: 'Latency Comparison (ms)' }
        },
        scales: {
          y: {
            beginAtZero: true,
            title: { display: true, text: 'Milliseconds (ms)' }
          }
        }
      }
    }
  
    if (latencyChart) latencyChart.destroy()
    latencyChart = new Chart(canvas, config)
  }
  