<script setup>
import FeatureCard from '../components/FeatureCard.vue'
const features = [
  {icon:'folder2-open', tag:'RESEARCH MANAGEMENT', title:'臨床研究資料管理',description:'整合研究計畫、受試者、臨床資訊與存活追蹤資料，讓研究紀錄清楚可追溯。'},
  {icon:'droplet-half', tag:'SAMPLE TRACKING', title:'檢體與樣本管理',description:'以受試者與研究計畫建立資料關聯，支援檢體紀錄、查詢與管理。'},
  {icon:'graph-up-arrow', tag:'TRANSCRIPTOMICS', title:'RNA-seq 差異表現分析',description:'串接 R / DESeq2 分析引擎，提供基因註解、火山圖與互動式 DEG Explorer。'}
]
</script>
<template>
  <div>
    <section class="hero-section">
      <div class="hero-glow hero-glow-one"></div><div class="hero-glow hero-glow-two"></div>
      <div class="container-xl hero-grid">
        <div class="hero-copy">
          <span class="eyebrow-pill"><span class="pulse-dot"></span> BIOINFORMATICS × CLINICAL RESEARCH</span>
          <h1>讓研究數據<br /><span>清晰明瞭</span></h1>
          <p class="hero-lead">從研究計畫、臨床與檢體資料管理，到 RNA-seq 差異表現分析，BioHub 讓複雜流程在同一個平台上更有條理</p>
          <div class="hero-actions">
            <RouterLink to="/features" class="btn btn-hero">探索平台功能 <i class="bi bi-arrow-right ms-2"></i></RouterLink>
            <RouterLink to="/about" class="btn btn-hero-outline">了解專題架構</RouterLink>
          </div>
          
        </div>
        <div class="hero-visual" aria-label="BioHub 介面示意">
          <div class="visual-orbit orbit-one"></div><div class="visual-orbit orbit-two"></div>
          <div class="visual-card main-visual-card">
            <div class="visual-header"><span class="visual-logo"><i class="bi bi-activity"></i></span><div><strong>Research workspace</strong><small>整合式研究工作流程</small></div><span class="visual-status">掌握流程</span></div>
            <div class="visual-divider"></div>
            <div class="workflow-stage"><span class="step-circle is-done"><i class="bi bi-check-lg"></i></span><div><strong>Research Projects</strong><small>研究計畫與受試者資料</small></div><span class="stage-pill">管理</span></div>
            <div class="workflow-line"></div>
            <div class="workflow-stage"><span class="step-circle is-done"><i class="bi bi-check-lg"></i></span><div><strong>Samples & Clinical Data</strong><small>關聯資料與檢體紀錄</small></div><span class="stage-pill">追蹤</span></div>
            <div class="workflow-line"></div>
            <div class="workflow-stage"><span class="step-circle is-current"><i class="bi bi-graph-up-arrow"></i></span><div><strong>RNA-seq Analysis</strong><small>DESeq2 · Gene Annotation · DEG Explorer</small></div><span class="stage-pill stage-pill-blue">分析</span></div>
            <div class="visual-bottom"><i class="bi bi-layers-half"></i> MVC · EF Core · SQL Server · Rscript</div>
          </div>
          <div class="float-card float-card-top"><span class="float-icon mint"><i class="bi bi-database-check"></i></span><div><strong>資料關聯管理</strong><small>Research / Subject / Sample</small></div></div>
          <div class="float-card float-card-bottom"><span class="float-icon blue"><i class="bi bi-dna"></i></span><div><strong>DESeq2 分析引擎</strong><small>R · Counts Matrix</small></div></div>
        </div>
      </div>
    </section>
    <section class="features-section">
      <div class="container-xl">
        <div class="section-heading"><div><span class="eyebrow">ONE PLATFORM · CONNECTED WORKFLOWS</span><h2>為研究工作打造的一站式平台</h2><p>將資料輸入、研究管理與分析結果串成清楚的工作流程。</p></div><RouterLink to="/features" class="section-action">查看所有功能 <i class="bi bi-arrow-right"></i></RouterLink></div>
        <div class="row g-4"><div v-for="feature in features" :key="feature.title" class="col-12 col-md-6 col-lg-4"><FeatureCard v-bind="feature" /></div></div>
      </div>
    </section>
    <section class="cta-section"><div class="container-xl"><div class="cta-panel"><div><span class="eyebrow">BUILT FOR BIOINFORMATICS</span><h2>從資料管理到分析結果，流程更一致。</h2><p>以熟悉的網頁介面，串接 C#、資料庫與 R 分析。</p></div><RouterLink to="/about" class="btn btn-light cta-button">了解技術架構 <i class="bi bi-arrow-right ms-2"></i></RouterLink></div></div></section>
  </div>
</template>
