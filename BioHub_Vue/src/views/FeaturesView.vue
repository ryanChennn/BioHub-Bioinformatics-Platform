<script setup>
import FeatureCard from '../components/FeatureCard.vue'
const features = [
  {icon:'folder2-open', tag:'PROJECTS', title:'研究計畫管理', description:'建立研究計畫、指定負責人，並關聯受試者與相關紀錄。'},
  {icon:'people', tag:'SUBJECTS', title:'受試者與臨床資料', description:'建立受試者資料，管理臨床與存活追蹤資訊。'},
  {icon:'droplet-half', tag:'SAMPLES', title:'檢體資料管理', description:'記錄檢體資訊並對應受試者及研究計畫。'},
  {icon:'grid-1x2', tag:'DASHBOARD', title:'Dashboard 總覽', description:'集中查看研究資料及分析工作的統計概況。'},
  {icon:'activity', tag:'RNA-SEQ', title:'DESeq2 差異表現分析', description:'從計數矩陣與樣本資訊出發，執行 R 分析並輸出差異表現結果。'},
  {icon:'funnel', tag:'DEG EXPLORER', title:'互動式基因結果探索', description:'透過 AJAX 進行基因搜尋、數值篩選、排序及 CSV 匯出。'}
]
</script>
<template>
  <div class="interior-page"><div class="container-xl"><div class="interior-header"><span class="eyebrow">PLATFORM CAPABILITIES</span><h1>平台功能</h1><p>BioHub 串聯研究資料管理與生物資訊分析，讓資料從建立、整理到結果探索更容易追蹤。</p></div><div class="row g-4 mb-5"><div v-for="feature in features" :key="feature.title" class="col-12 col-md-6 col-lg-4"><FeatureCard v-bind="feature"/></div></div><p class="small text-secondary">說明：此頁介紹 BioHub 專題模組。功能開放程度與操作權限依實際後台實作為準。</p></div></div>
</template>
