// import { createRouter, createWebHistory } from 'vue-router'
// import HomeView from '../views/HomeView.vue'
// import FeaturesView from '../views/FeaturesView.vue'
// import AboutView from '../views/AboutView.vue'

// const router = createRouter({
//   history: createWebHistory(import.meta.env.BASE_URL),
//   routes: [
//     { path: '/', name: 'home', component: HomeView },
//     { path: '/features', name: 'features', component: FeaturesView },
//     { path: '/about', name: 'about', component: AboutView },
//   ],
//   scrollBehavior() { return { top: 0 } }
// })

// export default router

import { createRouter, createWebHistory } from "vue-router";
import HomeView from "../views/HomeView.vue";
import FeaturesView from "../views/FeaturesView.vue";
import AboutView from "../views/AboutView.vue";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", name: "home", component: HomeView },
    { path: "/features", name: "features", component: FeaturesView },
    { path: "/about", name: "about", component: AboutView },
  ],
  scrollBehavior() {
    return { top: 0 };
  },
});

// 暫時加入以下診斷
console.log("Vite BASE_URL:", import.meta.env.BASE_URL);

router.afterEach((to, from, failure) => {
  console.log("目前路由:", to.fullPath);
  console.log(
    "匹配路由:",
    to.matched.map((r) => r.path),
  );
  console.log("路由錯誤:", failure);
});

router.onError((error) => {
  console.error("Vue Router 發生錯誤:", error);
});

export default router;
