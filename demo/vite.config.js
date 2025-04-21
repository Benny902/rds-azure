export default {
  server: {
    proxy: {
      '/monolith': {
        target: 'http://localhost:54402',
        changeOrigin: true,
        rewrite: path => path.replace(/^\/monolith/, '')
      },
      '/microservice': {
        target: 'http://localhost:64402',
        changeOrigin: true,
        rewrite: path => path.replace(/^\/microservice/, '')
      }
    }
  }
}
