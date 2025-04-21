let maps = new WeakMap()

export function initializeMap(container, lat, lon) {
    const mapElement = container.querySelector('.mapContainer')
    mapElement.classList.remove('hidden')
  
    let map = mapElement._leaflet_map
    if (!map) {
      map = L.map(mapElement).setView([lat, lon], 13)
      mapElement._leaflet_map = map
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
      }).addTo(map)
    } else {
      map.setView([lat, lon], 13)
    }
    return map
  }  

export function showOnMap(container, nameOrQuery) {
  const mapElement = container.querySelector('.mapContainer')

  fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(nameOrQuery)}`)
    .then(res => res.json())
    .then(data => {
      if (!data?.length) return

      const lat = parseFloat(data[0].lat)
      const lon = parseFloat(data[0].lon)

      const map = initializeMap(container, lat, lon)
      L.marker([lat, lon]).addTo(map).bindPopup(nameOrQuery).openPopup()
    })
}

export async function showStreetOnMap(container, streetName, localityName) {
  const queryUrl = `https://nominatim.openstreetmap.org/search?format=json&limit=1&addressdetails=1&street=${encodeURIComponent(streetName)}&city=${encodeURIComponent(localityName)}`

  const res = await fetch(queryUrl)
  const data = await res.json()

  const match = data?.[0]
  if (!match) return

  const lat = parseFloat(match.lat)
  const lon = parseFloat(match.lon)

  // Only initialize AFTER you have the coordinates
  const map = initializeMap(container, lat, lon)

  // Remove only markers, keep tile layer
  map.eachLayer(layer => {
    if (layer instanceof L.Marker) map.removeLayer(layer)
  })

  map.setView([lat, lon], 16)
  const marker = L.marker([lat, lon]).addTo(map)
  marker.bindPopup(`<strong>${streetName}</strong><br>${localityName}`).openPopup()
}
