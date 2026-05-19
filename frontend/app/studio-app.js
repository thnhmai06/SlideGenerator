/* ═══════════════════════════════════════════════════════════
   SlideGenerator — Studio app
═══════════════════════════════════════════════════════════ */

/* ─── STATE ─── */
const ST = {
  mainTab: 'studio',
  subTab: 'running',        /* configure · running · result */
  themeMode: 'light',       /* light · dark · system */

  /* Configure form */
  cfg: {
    recipe: 'rec_revenue',
    extension: 'pptx',
    useLocal: true,
    saveDl: true,
    saveEdit: false,
    imgPath: 'D:/Slides/Images/',
    localPath: 'D:/Data/Reports/',
  },
  recipeOpen: false,
  extOpen: false,

  /* Pre-populated mock data so tabs are immediately viewable */
  captured: CAPTURED_CFG,
  tree: makeTree(),

  /* Expanded states */
  open: { workflow: true, workbooks: { wb_1: false, wb_2: true, wb_3: false }, worksheets: { 'wb_2:ws_2_2': true } },
  openRow: null,             /* "wb:ws:rowIdx" */
};

/* ─── HELPERS ─── */
const $ = (id) => document.getElementById(id);
const el = (html) => {
  const d = document.createElement('div'); d.innerHTML = html.trim(); return d.firstElementChild;
};

/* ═══════════════════════════════════════════════════════════
   TOP BAR — Main tabs (Recipes / Studio)
═══════════════════════════════════════════════════════════ */
function renderMainTabs() {
  const recipes = document.querySelector('[data-tab="recipes"]');
  const studio  = document.querySelector('[data-tab="studio"]');
  recipes.innerHTML = `${svg('recipe', 16)} <span>Recipes</span>`;
  studio.innerHTML  = `${svg('studio', 16)} <span>Studio</span>`;
  recipes.classList.toggle('active', ST.mainTab === 'recipes');
  studio.classList.toggle('active',  ST.mainTab === 'studio');
  positionMainTabsIndicator();
}
function positionMainTabsIndicator() {
  const active = document.querySelector(`#mainTabs [data-tab="${ST.mainTab}"]`);
  const ind = $('mainTabsInd');
  if (!active || !ind) return;
  ind.style.left  = active.offsetLeft + 'px';
  ind.style.width = active.offsetWidth + 'px';
}
function setMainTab(tab) {
  if (tab === ST.mainTab) return;
  ST.mainTab = tab;
  renderMainTabs();
  renderPage();
}

/* ═══════════════════════════════════════════════════════════
   THEME SWITCHER (3 modes)
═══════════════════════════════════════════════════════════ */
function renderThemeSwitch() {
  const map = { light: 'sun', dark: 'moon', system: 'monitor' };
  document.querySelectorAll('#themeSwitch .ts-btn').forEach(btn => {
    btn.innerHTML = svg(map[btn.dataset.mode], 15);
    btn.classList.toggle('active', btn.dataset.mode === ST.themeMode);
  });
  const active = document.querySelector(`#themeSwitch [data-mode="${ST.themeMode}"]`);
  const ind = $('themeInd');
  if (active && ind) ind.style.left = active.offsetLeft + 'px';
}
function setThemeMode(mode) {
  ST.themeMode = mode;
  let applied = mode;
  if (mode === 'system') {
    applied = matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
  document.documentElement.setAttribute('data-theme', applied);
  renderThemeSwitch();
}

/* Top-right icons */
function renderTopRightIcons() {
  $('aboutBtn').innerHTML    = svg('info', 18);
  $('settingsBtn').innerHTML = svg('cog', 18);
}

/* ═══════════════════════════════════════════════════════════
   PAGE — switches by mainTab
═══════════════════════════════════════════════════════════ */
function renderPage() {
  const page = $('page');
  if (ST.mainTab === 'recipes') {
    page.innerHTML = `
      <div class="page-head">
        <h1 class="page-title">Recipes</h1>
        <p class="page-subtitle">Quản lý kho công thức (recipe) — định nghĩa cách dữ liệu được map thành slide.</p>
      </div>
      <div class="placeholder-pane">
        <div class="ph-icon">${svg('recipe', 36)}</div>
        <h3>Trình quản lý Recipe</h3>
        <p>Giao diện Recipes đang được phát triển. Hãy bắt đầu với một workflow ở tab <strong>Studio</strong> để chạy thử các recipe có sẵn.</p>
      </div>`;
    return;
  }

  /* Studio */
  page.innerHTML = `
    <div class="page-head">
      <h1 class="page-title">Studio</h1>
      <p class="page-subtitle">Cấu hình, theo dõi và xem kết quả các tiến trình tạo slide.</p>
    </div>
    <div class="subtabs" id="subTabs"></div>
    <div id="subPane"></div>`;
  renderSubTabs();
  renderSubPane();
}

/* ═══════════════════════════════════════════════════════════
   SUB TABS — Configure / Running / Result
═══════════════════════════════════════════════════════════ */
function renderSubTabs() {
  const wf = ST.tree?.workflow;
  const running = wf?.status === 'running' || wf?.status === 'paused';
  const done = wf?.status === 'done' || wf?.status === 'stopped' || (wf && !running);

  const tabs = [
    { id: 'configure', lbl: 'Cấu hình', ico: 'configure' },
    { id: 'running',   lbl: 'Đang chạy', ico: 'running', dot: running ? 'running' : '' },
    { id: 'result',    lbl: 'Kết quả',   ico: 'result',  dot: done && wf?.status !== 'running' ? 'done' : '' },
  ];

  $('subTabs').innerHTML = tabs.map(t => `
    <button class="subtab-btn${t.id === ST.subTab ? ' active' : ''}"
            onclick="setSubTab('${t.id}')">
      ${svg(t.ico, 16)}
      <span style="margin-left:7px;">${t.lbl}</span>
      ${t.dot ? `<span class="badge-dot ${t.dot}"></span>` : ''}
      <span class="subtab-bar"></span>
    </button>`).join('');
}
function setSubTab(id) {
  if (id === ST.subTab) return;
  ST.subTab = id;
  renderSubTabs();
  renderSubPane();
}

/* ═══════════════════════════════════════════════════════════
   SUB PANE dispatcher
═══════════════════════════════════════════════════════════ */
function renderSubPane() {
  const pane = $('subPane');
  if (ST.subTab === 'configure') pane.innerHTML = renderConfigure();
  else if (ST.subTab === 'running') pane.innerHTML = renderRunning();
  else if (ST.subTab === 'result')  pane.innerHTML = renderResult();

  /* Post-render: bind dropdowns */
  if (ST.subTab === 'configure') bindConfigureDropdowns();
}

/* ═══════════════════════════════════════════════════════════
   CONFIGURE
═══════════════════════════════════════════════════════════ */
function renderConfigure() {
  const c = ST.cfg;
  const recipe = RECIPES.find(r => r.id === c.recipe);
  const ext = EXTENSIONS.find(e => e.id === c.extension);
  const showImgPath = c.saveDl || c.saveEdit;

  return `
    <div class="card">
      <div class="card-head">
        <div>
          <div class="card-title">${svg('configure', 18)} Cấu hình tiến trình</div>
          <div class="card-desc">Chọn recipe, định dạng đầu ra và các tuỳ chọn lưu trữ trước khi bắt đầu.</div>
        </div>
      </div>

      <div style="display:grid;grid-template-columns:1.4fr 1fr;gap:var(--sp-5);">
        <!-- RECIPE -->
        <div class="field">
          <label class="field-label">Recipe</label>
          <div class="dropdown" id="ddRecipe">
            <button type="button" class="dropdown-trigger${ST.recipeOpen ? ' open' : ''}"
                    onclick="toggleDd('recipe')">
              ${svg('recipe', 16)}
              ${recipe
                ? `<span style="color:var(--tx);font-weight:700;">${recipe.name}</span>
                   <span style="margin-left:auto;font-size:var(--f-xs);color:var(--tx-mute);font-weight:500;">${recipe.desc}</span>`
                : `<span class="placeholder">Chọn công thức để tạo slide…</span>`}
              <span class="chev">${svg('chevronDown', 16)}</span>
            </button>
            ${ST.recipeOpen ? `
              <div class="dropdown-menu">
                <button class="dropdown-item${!c.recipe ? ' selected' : ''}" onclick="pickRecipe('')">
                  ${svg('recipe', 16)}
                  <div style="display:flex;flex-direction:column;gap:1px;flex:1;">
                    <span style="font-weight:700;color:var(--tx-mute);">Chọn Recipe</span>
                    <span style="font-size:var(--f-xs);color:var(--tx-dim);font-weight:500;">Chưa chọn công thức</span>
                  </div>
                </button>
                <div style="height:1px;background:var(--bd);margin:4px 0;"></div>
                ${RECIPES.map(r => `
                  <button class="dropdown-item${c.recipe === r.id ? ' selected' : ''}"
                          onclick="pickRecipe('${r.id}')">
                    ${svg('recipe', 16)}
                    <div style="display:flex;flex-direction:column;gap:1px;flex:1;">
                      <span style="font-weight:700;">${r.name}</span>
                      <span style="font-size:var(--f-xs);color:var(--tx-mute);font-weight:500;">${r.desc}</span>
                    </div>
                  </button>`).join('')}
              </div>` : ''}
          </div>
          <span class="field-hint">Recipe định nghĩa cách dữ liệu Excel được map vào slide.</span>
        </div>

        <!-- EXTENSION -->
        <div class="field">
          <label class="field-label">Định dạng đầu ra</label>
          <div class="dropdown" id="ddExt">
            <button type="button" class="dropdown-trigger${ST.extOpen ? ' open' : ''}"
                    onclick="toggleDd('ext')">
              ${svg('filePpt', 16)}
              <span style="color:var(--tx);font-weight:700;">${ext.desc}</span>
              <span style="margin-left:auto;font-size:var(--f-xs);color:var(--tx-mute);font-weight:500;">${ext.name}</span>
              <span class="chev">${svg('chevronDown', 16)}</span>
            </button>
            ${ST.extOpen ? `
              <div class="dropdown-menu">
                ${EXTENSIONS.map(e => `
                  <button class="dropdown-item${c.extension === e.id ? ' selected' : ''}"
                          onclick="pickExt('${e.id}')">
                    ${svg('filePpt', 16)}
                    <span style="font-weight:700;">${e.desc}</span>
                    <span class="item-sub">${e.name}</span>
                  </button>`).join('')}
              </div>` : ''}
          </div>
          <span class="field-hint">PowerPoint, template hoặc định dạng riêng của SlideGenerator.</span>
        </div>
      </div>

      <!-- TOGGLES -->
      <div class="col" style="margin-top:var(--sp-6);gap:var(--sp-3);">
        <div class="toggle-row${c.useLocal ? ' on' : ''}" style="cursor:pointer;" onclick="toggleCfg('useLocal')">
          <div class="toggle-info">
            <div class="toggle-name">${svg('folder', 18)} Cho phép truy cập file cục bộ</div>
            <div class="toggle-desc">Cho phép dữ liệu trong Workbook có thể liên kết tới file trên máy.</div>
          </div>
          <label class="toggle-sw" onclick="event.stopPropagation()">
            <input type="checkbox" ${c.useLocal ? 'checked' : ''}>
            <span class="slider"></span>
          </label>
        </div>

        <div style="display:grid;grid-template-columns:1fr 1fr;gap:var(--sp-3);">
          <div class="toggle-row${c.saveDl ? ' on' : ''}" style="cursor:pointer;" onclick="toggleCfg('saveDl')">
            <div class="toggle-info">
              <div class="toggle-name">${svg('download', 18)} Lưu ảnh đã tải xuống</div>
              <div class="toggle-desc">Giữ lại bản gốc các ảnh được tải từ Internet trong quá trình tạo slide.</div>
            </div>
            <label class="toggle-sw" onclick="event.stopPropagation()">
              <input type="checkbox" ${c.saveDl ? 'checked' : ''}>
              <span class="slider"></span>
            </label>
          </div>

          <div class="toggle-row${c.saveEdit ? ' on' : ''}" style="cursor:pointer;" onclick="toggleCfg('saveEdit')">
            <div class="toggle-info">
              <div class="toggle-name">${svg('edit', 18)} Lưu ảnh đã chỉnh sửa</div>
              <div class="toggle-desc">Lưu các ảnh đã qua xử lý (crop, resize, áp filter) phục vụ kiểm tra.</div>
            </div>
            <label class="toggle-sw" onclick="event.stopPropagation()">
              <input type="checkbox" ${c.saveEdit ? 'checked' : ''}>
              <span class="slider"></span>
            </label>
          </div>
        </div>

        ${showImgPath ? `
          <div class="field" style="margin-left:var(--sp-5);">
            <label class="field-label">Vị trí lưu ảnh</label>
            <div style="display:flex;gap:var(--sp-2);">
              <input class="input mono" placeholder="D:/Slides/Images/" value="${c.imgPath}"
                     oninput="setCfgRaw('imgPath',this.value)">
              <button class="btn btn-sec btn-sm" onclick="browseFolder('imgPath')">
                ${svg('folder', 15)} Duyệt
              </button>
            </div>
            <span class="field-hint">Vị trí lưu ảnh tải về và ảnh đã chỉnh sửa.</span>
          </div>` : ''}
      </div>

      <!-- CTA -->
      <div style="margin-top:var(--sp-8);display:flex;justify-content:center;">
        <button class="create-btn" id="createBtn" onclick="onCreate(this)" ${c.recipe ? '' : 'disabled style="opacity:.55;cursor:not-allowed;"'}>
          ${svg('create', 18)} <span>Tạo Slide</span>
        </button>
      </div>
      ${!c.recipe ? `<div class="field-hint" style="text-align:center;margin-top:var(--sp-3);">Chọn một recipe để bắt đầu tạo slide.</div>` : ''}
    </div>`;
}

/* Configure handlers */
function setCfg(k, v) { ST.cfg[k] = v; renderSubPane(); }
function setCfgRaw(k, v) { ST.cfg[k] = v; /* no re-render */ }
function toggleCfg(k) { setCfg(k, !ST.cfg[k]); }
function openRecipe(id) {
  const r = RECIPES.find(r => r.id === (id || ST.cfg.recipe));
  if (r) alert('Mở recipe "' + r.name + '" trong trình quản lý…');
}
function browseFolder(k) {
  /* mock: pretend we open a system dialog */
  const samples = { localPath: 'D:/Data/Reports/', imgPath: 'D:/Slides/Images/' };
  ST.cfg[k] = samples[k] || '';
  renderSubPane();
}
function toggleDd(which) {
  if (which === 'recipe') { ST.recipeOpen = !ST.recipeOpen; ST.extOpen = false; }
  else { ST.extOpen = !ST.extOpen; ST.recipeOpen = false; }
  renderSubPane();
}
function pickRecipe(id) { ST.cfg.recipe = id || ''; ST.recipeOpen = false; renderSubPane(); }
function pickExt(id)    { ST.cfg.extension = id; ST.extOpen = false; renderSubPane(); }

/* Close dropdowns on outside click */
function bindConfigureDropdowns() {
  document.addEventListener('click', closeDdOutside, { capture: true, once: true });
}
function closeDdOutside(e) {
  if (!e.target.closest('.dropdown') && (ST.recipeOpen || ST.extOpen)) {
    ST.recipeOpen = false; ST.extOpen = false;
    renderSubPane();
  } else {
    bindConfigureDropdowns();   /* re-arm */
  }
}

/* ═══════════════════════════════════════════════════════════
   CREATE — Tạo slide
═══════════════════════════════════════════════════════════ */
function onCreate(btn) {
  if (!ST.cfg.recipe) return;
  /* Sparkle burst */
  for (let i = 0; i < 8; i++) {
    const angle = (Math.PI * 2 / 8) * i;
    const dist = 36 + Math.random() * 16;
    const dx = Math.cos(angle) * dist + 'px';
    const dy = Math.sin(angle) * dist + 'px';
    const s = document.createElement('span');
    s.className = 'spark';
    s.style.setProperty('--dx', dx);
    s.style.setProperty('--dy', dy);
    btn.appendChild(s);
    setTimeout(() => s.remove(), 620);
  }
  setTimeout(showExternalWarning, 420);
}

function showExternalWarning() {
  $('modalMount').innerHTML = `
    <div class="modal-scrim" onclick="if(event.target===this) closeModal()">
      <div class="modal">
        <div class="modal-icon">${svg('warning', 30)}</div>
        <div class="modal-title">Cảnh báo liên kết bên ngoài</div>
        <div class="modal-body">
          Tệp tin này chứa các liên kết hình ảnh từ <strong>nguồn bên ngoài</strong>.
          Việc tiếp tục đồng nghĩa với việc bạn chấp nhận các rủi ro về bảo mật khi
          tải xuống dữ liệu từ Internet.
          <br><br>
          Bạn có muốn tiếp tục tải ảnh không?
        </div>
        <div class="modal-actions">
          <button class="btn btn-sec" onclick="closeModal()">Hủy bỏ</button>
          <button class="btn btn-pri" onclick="confirmCreate()">${svg('create', 15)} Tiếp tục</button>
        </div>
      </div>
    </div>`;
}
function closeModal() { $('modalMount').innerHTML = ''; }

function confirmCreate() {
  closeModal();
  /* Snapshot config */
  const recipe = RECIPES.find(r => r.id === ST.cfg.recipe);
  ST.captured = {
    recipe:    ST.cfg.recipe,
    recipeName:recipe?.name ?? '',
    recipePath:`C:/Users/admin/Documents/SlideGenerator/Recipes/${ST.cfg.recipe}.recipe`,
    extension: ST.cfg.extension,
    useLocal:  ST.cfg.useLocal,
    localPath: ST.cfg.localPath || 'D:/Data/Reports/',
    saveDl:    ST.cfg.saveDl,
    saveEdit:  ST.cfg.saveEdit,
    imgPath:   ST.cfg.imgPath || 'D:/Slides/Images/',
  };
  /* Build tree */
  ST.tree = makeTree();
  ST.open.workflow = true;
  ST.open.workbooks = { wb_1: false, wb_2: true, wb_3: false };
  ST.open.worksheets = { 'wb_2:ws_2_2': true };
  ST.subTab = 'running';
  renderSubTabs();
  renderSubPane();
}

/* ═══════════════════════════════════════════════════════════
   RUNNING — 4-level hierarchy
═══════════════════════════════════════════════════════════ */
function renderRunning() {
  if (!ST.tree) return `
    <div class="placeholder-pane">
      <div class="ph-icon">${svg('running', 36)}</div>
      <h3>Chưa có tiến trình nào đang chạy</h3>
      <p>Hãy vào tab <strong>Cấu hình</strong>, chọn recipe và nhấn <strong>Tạo Slide</strong> để bắt đầu.</p>
    </div>`;
  return renderHierarchy(false);
}

function renderResult() {
  if (!ST.tree) return `
    <div class="placeholder-pane">
      <div class="ph-icon">${svg('result', 36)}</div>
      <h3>Chưa có kết quả</h3>
      <p>Kết quả sẽ xuất hiện ở đây sau khi một workflow hoàn tất hoặc bị dừng.</p>
    </div>`;
  return renderHierarchy(true);
}

/* shared renderer — readonly toggles actions */
function renderHierarchy(readonly) {
  const wf = ST.tree.workflow;
  const c = ST.captured;
  const ext = EXTENSIONS.find(e => e.id === c.extension);

  /* Workflow status & stats */
  const doneCount = wf.workbooks.filter(w => w.status === 'done').length;
  const totalProgress = readonly ? 100 : wf.progress;
  const wfStatus = readonly ? 'done' : wf.status;

  const cfgGrid = `
    <div class="cfg-summary">
      <div class="cfg-item">
        <span class="cfg-key">Recipe</span>
        <div class="cfg-vals">
          <span class="cfg-val">${c.recipeName}
            <button class="cfg-open-btn" onclick="alert('Mở Recipe trong trình quản lý…')" style="margin-left:auto;">
              ${svg('external', 14)} Mở
            </button>
          </span>
          <span class="cfg-val mono cfg-path" onclick="copyPath('${c.recipePath}')" title="Click để sao chép">${c.recipePath}</span>
        </div>
      </div>
      <div class="cfg-item">
        <span class="cfg-key">Định dạng đầu ra</span>
        <div class="cfg-vals">
          <span class="cfg-val">${svg('filePpt', 14)} ${ext?.desc ?? c.extension}<span class="cfg-val-desc">${ext?.name ?? ''}</span></span>
        </div>
      </div>
      <div class="cfg-item">
        <span class="cfg-key">Cho phép truy cập file cục bộ</span>
        <div class="cfg-vals">
          <span class="cfg-val">${c.useLocal ? '✓ Bật' : '— Tắt'}</span>
        </div>
      </div>
      <div class="cfg-item">
        <span class="cfg-key">Vị trí lưu ảnh</span>
        <div class="cfg-vals">
          ${(c.saveDl || c.saveEdit) ? `<span class="cfg-val mono">
            <span class="cfg-path" onclick="copyPath('${c.imgPath}')" title="Click để sao chép">${c.imgPath}</span>
            <button class="cfg-open-btn" onclick="alert('Mở thư mục ảnh…')" style="margin-left:auto;">
              ${svg('folderOpen', 14)} Mở
            </button>
          </span>` : `<span class="cfg-val tx-dim">—</span>`}
        </div>
      </div>
    </div>`;

  /* Workflow node */
  const wfOpen = ST.open.workflow;
  const wfActions = readonly
    ? `<div class="node-actions">
        <button class="icon-btn" title="Xóa workflow này" onclick="event.stopPropagation();clearWorkflow()" style="color:var(--da);"><svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><line x1='18' y1='6' x2='6' y2='18'/><line x1='6' y1='6' x2='18' y2='18'/></svg></button>
      </div>`
    : `<div class="node-actions">
        ${wf.status === 'paused'
          ? `<button class="icon-btn" title="Tiếp tục" onclick="event.stopPropagation();toggleWfPlay()">${svg('play', 16)}</button>`
          : `<button class="icon-btn" title="Tạm dừng" onclick="event.stopPropagation();toggleWfPlay()">${svg('pause', 16)}</button>`}
        <button class="icon-btn" title="Dừng workflow" onclick="event.stopPropagation();stopWf()">${svg('stop', 16)}</button>
      </div>`;

  return `
    <div class="node ${wfOpen ? 'open' : ''}" id="node-wf">
      <div class="node-head" onclick="toggleOpen('workflow')">
        <span class="node-chev">${svg('chevron', 16)}</span>
        <span class="node-icon">${svg('workflow', 16)}</span>
        <div class="node-meta">
          <div class="node-name">${wf.name} ${pill(wfStatus)}</div>
          <div class="node-sub">${wf.ts}</div>
        </div>
        <div class="node-progress"><div class="progress ${progClass(wfStatus)}"><span style="width:${totalProgress}%"></span></div></div>
        <div class="node-stats">
          <span class="node-count"><strong>${doneCount}</strong>/${wf.workbooks.length} workbook</span>
        </div>
        ${wfActions}
      </div>
      <div class="node-body">
        <div class="cfg-section-label">Properties</div>
        ${cfgGrid}
        ${renderLogs(wf.logs, 'Log của workflow')}
        <div style="margin-top:var(--sp-5);">
          ${wf.workbooks.map(wb => renderWorkbook(wb, readonly)).join('')}
        </div>
      </div>
    </div>`;
}

function renderWorkbook(wb, readonly) {
  const open = ST.open.workbooks[wb.id];
  const wbActions = readonly ? '' : `
    <div class="node-actions">
      ${wb.status === 'paused'
        ? `<button class="icon-btn" title="Tiếp tục" onclick="event.stopPropagation();toggleNodePlay('wb','${wb.id}')">${svg('play', 14)}</button>`
        : wb.status === 'running' ? `<button class="icon-btn" title="Tạm dừng" onclick="event.stopPropagation();toggleNodePlay('wb','${wb.id}')">${svg('pause', 14)}</button>` : ''}
      ${(wb.status === 'running' || wb.status === 'paused') ?
        `<button class="icon-btn" title="Dừng" onclick="event.stopPropagation();stopNode('wb','${wb.id}')">${svg('stop', 14)}</button>` : ''}
    </div>`;

  return `
    <div class="node lv2 ${open ? 'open' : ''}" id="node-wb-${wb.id}">
      <div class="node-head" onclick="toggleOpen('wb:${wb.id}')">
        <span class="node-chev">${svg('chevron', 14)}</span>
        <span class="node-icon">${svg('workbook', 14)}</span>
        <div class="node-meta">
          <div class="node-name">${wb.name} ${pill(wb.status)}</div>
          <div class="node-sub cfg-path" onclick="event.stopPropagation();copyPath('${wb.path}')" title="Click để sao chép" style="cursor:pointer;">${wb.path}</div>
        </div>
        <div class="node-progress"><div class="progress ${progClass(wb.status)}"><span style="width:${wb.progress}%"></span></div></div>
        <div class="node-stats">
          <span class="node-count"><strong>${wb.worksheets.filter(s => s.status === 'done').length}</strong>/${wb.worksheets.length} sheet</span>
        </div>
        ${wbActions}
      </div>
      <div class="node-body">
        ${readonly ? renderLogs(wb.logs, 'Log của workbook') : ''}
        <div style="margin-top:${readonly ? 'var(--sp-4)' : '0'};">
          ${wb.worksheets.map(ws => renderWorksheet(wb.id, ws, readonly)).join('')}
        </div>
      </div>
    </div>`;
}

function renderWorksheet(wbId, ws, readonly) {
  const key = `${wbId}:${ws.id}`;
  const open = ST.open.worksheets[key];
  const wsActions = readonly ? '' : `
    <div class="node-actions">
      ${ws.status === 'paused'
        ? `<button class="icon-btn" title="Tiếp tục" onclick="event.stopPropagation();toggleNodePlay('ws','${key}')">${svg('play', 14)}</button>`
        : ws.status === 'running' ? `<button class="icon-btn" title="Tạm dừng" onclick="event.stopPropagation();toggleNodePlay('ws','${key}')">${svg('pause', 14)}</button>` : ''}
      ${(ws.status === 'running' || ws.status === 'paused') ?
        `<button class="icon-btn" title="Dừng" onclick="event.stopPropagation();stopNode('ws','${key}')">${svg('stop', 14)}</button>` : ''}
    </div>`;

  const doneRows = ws.rows.filter(r => r.status === 'done' || r.status === 'error').length;

  return `
    <div class="node lv3 ${open ? 'open' : ''}" id="node-ws-${key.replace(':', '-')}">
      <div class="node-head" onclick="toggleOpen('ws:${key}')">
        <span class="node-chev">${svg('chevron', 12)}</span>
        <span class="node-icon">${svg('worksheet', 12)}</span>
        <div class="node-meta">
          <div class="node-name">${ws.name} ${pill(ws.status)}</div>
          <div class="node-sub">${ws.rows.length} hàng</div>
        </div>
        <div class="node-progress"><div class="progress ${progClass(ws.status)}"><span style="width:${ws.progress}%"></span></div></div>
        <div class="node-stats">
          <span class="node-count"><strong>${doneRows}</strong>/${ws.rows.length} hàng</span>
        </div>
        ${wsActions}
      </div>
      <div class="node-body">
        <div class="row-list">
          ${ws.rows.map(r => renderRow(wbId, ws.id, r)).join('')}
        </div>
      </div>
    </div>`;
}

function renderRow(wbId, wsId, row) {
  const key = `${wbId}:${wsId}:${row.idx}`;
  const open = ST.openRow === key;
  const safeKey = key.replace(/:/g, '-');
  const hasLogs = row.logs?.length > 0;
  return `
    <div class="row-item ${open ? 'open' : ''}" id="row-${safeKey}" onclick="toggleRow('${key}')">
      <span class="row-index">#${String(row.idx).padStart(3, '0')}</span>
      <span class="row-desc">${row.desc}</span>
      ${pill(row.status)}
    </div>
    <div class="row-logs" id="rowlogs-${safeKey}"${open ? '' : ' style="display:none"'}>
      ${hasLogs ? `
        <div style="display:flex;justify-content:flex-end;padding:2px 0 6px;">
          <button class="btn btn-xs btn-sec" onclick="event.stopPropagation();copyLogs('rowlog-${safeKey}')" title="Sao chép log" style="gap:5px;">${svg('copy', 11)} Sao chép</button>
        </div>
        <div class="log-block" id="rowlog-${safeKey}">${renderLogLines(row.logs)}</div>
      ` : renderLogLines(row.logs)}
    </div>`;
}

/* ─── shared bits ─── */
function pill(status) {
  const map = {
    running:   { cls: 'pill-running',   lbl: 'Đang chạy' },
    done:      { cls: 'pill-done',      lbl: 'Hoàn tất' },
    paused:    { cls: 'pill-paused',    lbl: 'Tạm dừng' },
    error:     { cls: 'pill-error',     lbl: 'Lỗi' },
    pending:   { cls: 'pill-pending',   lbl: 'Chờ' },
    stopped:   { cls: 'pill-cancelled', lbl: 'Đã dừng' },
    cancelled: { cls: 'pill-cancelled', lbl: 'Đã hủy' },
  };
  const m = map[status] ?? map.pending;
  return `<span class="pill ${m.cls}"><span class="dot"></span>${m.lbl}</span>`;
}
function progClass(status) {
  if (status === 'done') return 'done';
  if (status === 'error' || status === 'stopped') return 'error';
  if (status === 'paused') return 'paused';
  return '';
}
function renderLogs(logs, label) {
  if (!logs?.length) return `<div class="log-empty">${label} — chưa có log.</div>`;
  const logId = 'log-' + Math.random().toString(36).slice(2, 9);
  return `
    <div style="margin-top:var(--sp-2);">
      <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:8px;">
        <div style="font-size:var(--f-xs);font-weight:800;letter-spacing:.08em;text-transform:uppercase;color:var(--tx-dim);">${label}</div>
        <button class="btn btn-xs btn-sec" onclick="copyLogs('${logId}')" title="Sao chép toàn bộ log" style="gap:5px;">${svg('copy', 11)} Sao chép</button>
      </div>
      <div class="log-block" id="${logId}">${renderLogLines(logs)}</div>
    </div>`;
}
function renderLogLines(logs) {
  if (!logs?.length) return `<div class="log-empty">Không có log.</div>`;
  return logs.map(l => `
    <div class="log-line">
      <span class="log-time">${l.t}</span>
      <span class="log-lvl ${l.l.toLowerCase()}">${l.l}</span>
      <span class="log-msg">${l.m}</span>
    </div>`).join('');
}

/* Toggles — DOM-only, no full re-render */
function toggleOpen(key) {
  if (key === 'workflow') {
    ST.open.workflow = !ST.open.workflow;
    const node = document.getElementById('node-wf');
    if (node) node.classList.toggle('open', ST.open.workflow);
  } else if (key.startsWith('wb:')) {
    const id = key.slice(3);
    ST.open.workbooks[id] = !ST.open.workbooks[id];
    const node = document.getElementById('node-wb-' + id);
    if (node) node.classList.toggle('open', ST.open.workbooks[id]);
  } else if (key.startsWith('ws:')) {
    const id = key.slice(3);
    ST.open.worksheets[id] = !ST.open.worksheets[id];
    const node = document.getElementById('node-ws-' + id.replace(':', '-'));
    if (node) node.classList.toggle('open', ST.open.worksheets[id]);
  }
}
function toggleRow(key) {
  const prev = ST.openRow;
  ST.openRow = (ST.openRow === key) ? null : key;
  if (prev) {
    const pk = prev.replace(/:/g, '-');
    const ri = document.getElementById('row-' + pk);
    const rl = document.getElementById('rowlogs-' + pk);
    if (ri) ri.classList.remove('open');
    if (rl) rl.style.display = 'none';
  }
  if (ST.openRow) {
    const ck = ST.openRow.replace(/:/g, '-');
    const ri = document.getElementById('row-' + ck);
    const rl = document.getElementById('rowlogs-' + ck);
    if (ri) ri.classList.add('open');
    if (rl) rl.style.display = 'block';
  }
}

/* Clear workflow (Result tab) */
function clearWorkflow() {
  if (!confirm('Xóa workflow này? Không thể hoàn tác.')) return;
  ST.tree = null;
  ST.captured = null;
  ST.open = { workflow: true, workbooks: {}, worksheets: {} };
  ST.openRow = null;
  renderSubTabs();
  renderSubPane();
}

/* Workflow controls (mock) */
function toggleWfPlay() {
  const wf = ST.tree.workflow;
  wf.status = (wf.status === 'paused') ? 'running' : 'paused';
  renderSubTabs(); renderSubPane();
}
function stopWf() {
  if (!confirm('Dừng workflow này? Tiến trình sẽ chuyển sang tab Kết quả.')) return;
  ST.tree.workflow.status = 'stopped';
  ST.subTab = 'result';
  renderSubTabs(); renderSubPane();
}
function toggleNodePlay(kind, id) {
  if (kind === 'wb') {
    const wb = ST.tree.workflow.workbooks.find(w => w.id === id);
    wb.status = (wb.status === 'paused') ? 'running' : 'paused';
  } else if (kind === 'ws') {
    const [wbId, wsId] = id.split(':');
    const ws = ST.tree.workflow.workbooks.find(w => w.id === wbId).worksheets.find(s => s.id === wsId);
    ws.status = (ws.status === 'paused') ? 'running' : 'paused';
  }
  renderSubPane();
}
function stopNode(kind, id) {
  if (!confirm('Dừng node này?')) return;
  if (kind === 'wb') {
    const wb = ST.tree.workflow.workbooks.find(w => w.id === id);
    wb.status = 'stopped';
  } else if (kind === 'ws') {
    const [wbId, wsId] = id.split(':');
    const ws = ST.tree.workflow.workbooks.find(w => w.id === wbId).worksheets.find(s => s.id === wsId);
    ws.status = 'stopped';
  }
  renderSubPane();
}

function copyPath(path) {
  navigator.clipboard.writeText(path)
    .then(() => flashMsg('Đã sao chép đường dẫn ✓'))
    .catch(() => {
      const ta = document.createElement('textarea');
      ta.value = path; ta.style.cssText = 'position:fixed;opacity:0;';
      document.body.appendChild(ta); ta.select();
      document.execCommand('copy'); ta.remove();
      flashMsg('Đã sao chép đường dẫn ✓');
    });
}

function copyLogs(logId) {
  const block = document.getElementById(logId);
  if (!block) return;
  const text = [...block.querySelectorAll('.log-line')].map(line => {
    const t = (line.querySelector('.log-time')?.textContent ?? '').trim();
    const l = (line.querySelector('.log-lvl')?.textContent ?? '').trim().padEnd(7);
    const m = (line.querySelector('.log-msg')?.textContent ?? '').trim();
    return `${t}  ${l}  ${m}`;
  }).join('\n');
  navigator.clipboard.writeText(text)
    .then(() => flashMsg('Đã sao chép log ✓'))
    .catch(() => {
      const ta = document.createElement('textarea');
      ta.value = text; ta.style.cssText = 'position:fixed;opacity:0;';
      document.body.appendChild(ta); ta.select();
      document.execCommand('copy'); ta.remove();
      flashMsg('Đã sao chép log ✓');
    });
}

/* Flash msg */
let flashTimer;
function flashMsg(msg) {
  clearTimeout(flashTimer);
  const t = document.createElement('div');
  t.style.cssText = `
    position:fixed;top:80px;left:50%;transform:translateX(-50%);
    background:var(--tx);color:var(--bg);padding:10px 18px;border-radius:999px;
    font-size:var(--f-sm);font-weight:700;box-shadow:var(--shadow-md);
    z-index:2000;animation:dot-pulse 1.6s ease infinite;`;
  t.textContent = msg;
  document.body.appendChild(t);
  flashTimer = setTimeout(() => t.remove(), 2400);
}

/* ═══════════════════════════════════════════════════════════
   INIT
═══════════════════════════════════════════════════════════ */
renderMainTabs();
renderThemeSwitch();
renderTopRightIcons();
renderPage();

/* Re-position indicators after fonts load */
window.addEventListener('load', () => {
  positionMainTabsIndicator();
  renderThemeSwitch();
});
window.addEventListener('resize', () => {
  positionMainTabsIndicator();
  renderThemeSwitch();
});
