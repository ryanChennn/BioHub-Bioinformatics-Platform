(() => {
  'use strict';
  const root = document.getElementById('volcanoExplorer');
  if (!root) return;
  const plot = document.getElementById('volcanoPlot');
  const message = document.getElementById('volcanoMessage');
  const padjInput = document.getElementById('volcanoPadj');
  const fcInput = document.getElementById('volcanoFc');
  const reset = document.getElementById('volcanoReset');
  const geneSearch = document.getElementById('volcanoGeneSearch');
  const searchButton = document.getElementById('volcanoSearchBtn');
  const clearButton = document.getElementById('volcanoClearSearch');
  const searchResult = document.getElementById('volcanoSearchResult');
  if (![plot, message, padjInput, fcInput, reset, geneSearch, searchButton, clearButton, searchResult].every(Boolean)) {
    console.error('BioHub Volcano: 部分 HTML 元件 ID 不存在');
    return;
  }

  let points = [];
  let highlightedGenes = [];
  const colors = { up: '#ef476f', down: '#3388e6', other: '#a8b4c7' };

  function makeTrace(name, rows, color, size = 6) {
    return {
      type: 'scattergl', mode: 'markers', name,
      x: rows.map(g => g.log2FoldChange),
      y: rows.map(g => g.negLog10Padj),
      customdata: rows.map(g => [g.geneId, g.geneSymbol, g.padj, g.log2FoldChange]),
      marker: { color, size, opacity: name === '搜尋結果' ? 1 : 0.74 },
      hovertemplate: 'GeneID: %{customdata[0]}<br>' +
        'GeneSymbol: %{customdata[1]}<br>' +
        'log2FC: %{customdata[3]:.3f}<br>' +
        'padj: %{customdata[2]:.3g}<extra></extra>'
    };
  }

  async function render() {
    // 空欄位不可當成 0；請先輸入合法數字。
    if (!padjInput.value.trim() || !fcInput.value.trim()) {
      message.textContent = '請輸入 padj 與 |log2FC| 門檻。';
      return;
    }
    const padjCutoff = Number(padjInput.value);
    const fcCutoff = Number(fcInput.value);
    if (!Number.isFinite(padjCutoff) || padjCutoff <= 0 || padjCutoff > 1 ||
        !Number.isFinite(fcCutoff) || fcCutoff < 0) {
      message.textContent = '門檻錯誤：padj 需大於 0 且不超過 1；|log2FC| 不可小於 0。';
      return;
    }
    const up = [], down = [], other = [];
    for (const g of points) {
      // fcCutoff=0 時，FC=0 的資料避免同時被歸類成上調/下調
      if (g.padj < padjCutoff && g.log2FoldChange >= fcCutoff && g.log2FoldChange > 0) up.push(g);
      else if (g.padj < padjCutoff && g.log2FoldChange <= -fcCutoff && g.log2FoldChange < 0) down.push(g);
      else other.push(g);
    }
    message.textContent = `有效基因 ${points.length.toLocaleString('zh-TW')} 筆，向上 ${up.length}、向下 ${down.length}、其他 ${other.length}。`;
    const traces = [
      makeTrace('其他', other, colors.other),
      makeTrace('向下調控', down, colors.down),
      makeTrace('向上調控', up, colors.up),
      makeTrace('搜尋結果', highlightedGenes, '#f59e0b', 13)
    ];
    const layout = {
      autosize: true, height: 510,
      paper_bgcolor: '#ffffff', plot_bgcolor: '#ffffff',
      font: { family: 'Microsoft JhengHei, Noto Sans TC, sans-serif' },
      margin: { l: 65, r: 24, t: 45, b: 68 },
      xaxis: { title: { text: 'log2FoldChange' }, zeroline: true, gridcolor: '#eef2f7' },
      yaxis: { title: { text: '-log10(padj)' }, rangemode: 'tozero', gridcolor: '#eef2f7' },
      legend: { orientation: 'h', y: 1.16, x: 0 },
      hovermode: 'closest',
      shapes: [
        { type: 'line', x0: -fcCutoff, x1: -fcCutoff, y0: 0, y1: 1, yref: 'paper', line: { color: '#94a3b8', dash: 'dot', width: 1 } },
        { type: 'line', x0: fcCutoff, x1: fcCutoff, y0: 0, y1: 1, yref: 'paper', line: { color: '#94a3b8', dash: 'dot', width: 1 } },
        { type: 'line', x0: 0, x1: 1, xref: 'paper', y0: -Math.log10(padjCutoff), y1: -Math.log10(padjCutoff), line: { color: '#94a3b8', dash: 'dot', width: 1 } }
      ],
      uirevision: 'biohub-volcano'
    };
    await Plotly.react(plot, traces, layout, {
      responsive: true, displaylogo: false,
      toImageButtonOptions: { format: 'png', filename: 'BioHub_VolcanoPlot', width: 1200, height: 800, scale: 1 }
    });
  }

  async function searchGene() {
    const typed = geneSearch.value.trim();
    const keyword = typed.toLocaleLowerCase();
    if (!keyword) {
      highlightedGenes = [];
      searchResult.textContent = '請輸入 GeneSymbol 或 GeneID。';
      await render();
      return;
    }
    const matches = points.filter(g =>
      String(g.geneSymbol || '').toLocaleLowerCase().includes(keyword) ||
      String(g.geneId || '').toLocaleLowerCase().includes(keyword)
    );
    highlightedGenes = matches.slice(0, 30);
    if (!matches.length) {
      searchResult.textContent = `找不到符合「${typed}」的基因。請先確認 CSV 中是否有 GeneSymbol 或 GeneID。`;
    } else {
      const first = matches[0];
      searchResult.textContent = `找到 ${matches.length} 筆；第一筆基因資料為：${first.geneSymbol || first.geneId || '未命名'}，log2FC = ${Number(first.log2FoldChange).toFixed(3)}，padj = ${Number(first.padj).toPrecision(3)}。`;
    }    //圖上橘色亮點共 ${highlightedGenes.length} 筆。
    await render();
  }

  async function init() {
    if (typeof Plotly === 'undefined') {
      message.textContent = '尚未載入 Plotly.js；請檢查 CDN 或改用本機檔案。';
      return;
    }
    try {
      const response = await fetch(root.dataset.url, {
        credentials: 'same-origin', headers: { Accept: 'application/json' }, cache: 'no-store'
      });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const data = await response.json();
      if (!Array.isArray(data.points)) throw new Error('API 格式錯誤：缺少 points 陣列');
      points = data.points.filter(g => Number.isFinite(g.log2FoldChange) && Number.isFinite(g.negLog10Padj) && Number.isFinite(g.padj));
      if (!points.length) { message.textContent = '目前沒有可繪製的基因資料。'; return; }
      await render();
    } catch (error) {
      console.error(error);
      message.textContent = '無法載入 Volcano Plot，請檢查 API 回應與 Console 錯誤。';
    }
  }

  padjInput.addEventListener('change', () => { if (points.length) void render(); });
  fcInput.addEventListener('change', () => { if (points.length) void render(); });
  reset.addEventListener('click', () => {
    padjInput.value = '0.05'; fcInput.value = '1';
    if (points.length) void render();
  });
  searchButton.addEventListener('click', () => { if (points.length) void searchGene(); });
  geneSearch.addEventListener('keydown', event => {
    if (event.key === 'Enter') { event.preventDefault(); if (points.length) void searchGene(); }
  });
  clearButton.addEventListener('click', () => {
    geneSearch.value = ''; highlightedGenes = [];
    searchResult.textContent = '搜尋已清除。';
    if (points.length) void render();
  });
  void init();
})();
