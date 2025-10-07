import HomeView from '@/views/HomeView.vue'
import WeatherHome from '@/views/weatherApp/WeatherHome.vue'
import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/weather',
      name: 'weatherHome',
      component: WeatherHome,
    }
  ],
})

export default router
