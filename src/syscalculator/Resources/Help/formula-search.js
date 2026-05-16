(() => {
  if (window.syscalFormulaSearchInstalled) return;
  window.syscalFormulaSearchInstalled = true;
  window.syscalFormulaSearchMarks = [];
  window.syscalFormulaSearchIndex = -1;
  const style = document.createElement('style');
  style.textContent = 'mark.syscal-search{background:#fde68a;color:#111827;border-radius:3px;padding:0 2px}mark.syscal-search-current{background:#f59e0b;color:#111827}';
  document.head.appendChild(style);
  function clearMarks() {
    for (const mark of Array.from(document.querySelectorAll('mark.syscal-search'))) {
      const text = document.createTextNode(mark.textContent || '');
      mark.replaceWith(text);
      text.parentNode && text.parentNode.normalize();
    }
    window.syscalFormulaSearchMarks = [];
    window.syscalFormulaSearchIndex = -1;
  }
  function walk(node, query) {
    if (!node || !query) return;
    if (node.nodeType === Node.TEXT_NODE) {
      const text = node.nodeValue || '';
      const index = text.toLocaleLowerCase().indexOf(query);
      if (index < 0) return;
      const after = node.splitText(index);
      const tail = after.splitText(query.length);
      const mark = document.createElement('mark');
      mark.className = 'syscal-search';
      mark.textContent = after.nodeValue;
      after.replaceWith(mark);
      window.syscalFormulaSearchMarks.push(mark);
      walk(tail, query);
      return;
    }
    if (node.nodeType !== Node.ELEMENT_NODE) return;
    const tag = node.tagName;
    if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'MARK') return;
    for (const child of Array.from(node.childNodes)) walk(child, query);
  }
  function select(index) {
    const marks = window.syscalFormulaSearchMarks;
    for (const mark of marks) mark.classList.remove('syscal-search-current');
    if (!marks.length) return -1;
    const bounded = ((index % marks.length) + marks.length) % marks.length;
    const current = marks[bounded];
    current.classList.add('syscal-search-current');
    current.scrollIntoView({ block: 'center', inline: 'nearest' });
    window.syscalFormulaSearchIndex = bounded;
    return bounded;
  }
  window.syscalFormulaSearch = (query, preferredIndex) => {
    clearMarks();
    const q = (query || '').trim().toLocaleLowerCase();
    if (!q) return -1;
    walk(document.body, q);
    return select(preferredIndex >= 0 ? preferredIndex : 0);
  };
  window.syscalFormulaMoveSearch = (query, offset) => {
    const q = (query || '').trim();
    if (!q) return -1;
    if (!window.syscalFormulaSearchMarks.length) window.syscalFormulaSearch(q, 0);
    return select(window.syscalFormulaSearchIndex + offset);
  };
})();
