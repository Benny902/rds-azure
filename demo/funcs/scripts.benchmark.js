export async function runBenchmark(endpoint, correlationId, virtualUsers, durationSec) {
    const results = []
    const endTime = Date.now() + durationSec * 1000
  
    const runRequest = async () => {
      const start = performance.now()
      try {
        const res = await fetch(endpoint, { method: 'GET', headers: { 'X-Correlation-ID': correlationId } })
        const end = performance.now()
        results.push({ status: res.status, duration: end - start, success: res.ok })
      } catch {
        results.push({ status: 0, duration: 0, success: false })
      }
    }
  
    while (Date.now() < endTime) {
      const batch = []
      for (let i = 0; i < virtualUsers; i++) batch.push(runRequest())
      await Promise.all(batch)
    }
  
    return results
  }
  
  export function summarizeBenchmark(results, durationSec) {
    const durations = results.filter(r => r.success).map(r => r.duration).sort((a, b) => a - b)
    const percentile = p => durations[Math.floor(p / 100 * durations.length)]?.toFixed(2) || '0'
    const total = results.length
    const success = results.filter(r => r.success).length
    const failed = total - success
    const avg = (durations.reduce((a, b) => a + b, 0) / durations.length || 0).toFixed(2)
    const min = Math.min(...durations).toFixed(2)
    const max = Math.max(...durations).toFixed(2)
    const rps = (success / durationSec).toFixed(2)
  
    return { total, success, failed, min_ms: min, max_ms: max, avg_ms: avg, rps, p50: percentile(50), p90: percentile(90), p99: percentile(99) }
  }
  