
document.addEventListener("DOMContentLoaded", () => {

    const root = document.getElementById("degExplorer");

    // 尚未完成分析的頁面沒有 DEG Explorer
    if (!root) return;

    const jobId = root.dataset.jobId;
    const apiUrl = root.dataset.apiUrl;

    const tableHead = document.getElementById("degTableHead");
    const tableBody = document.getElementById("degTableBody");
    const filterPanel = document.getElementById("degFilterPanel");
    const totalCountElement = document.getElementById("degTotalCount");
    const pageInfo = document.getElementById("degPageInfo");
    const pageSizeElement = document.getElementById("degPageSize");
    const previousButton = document.getElementById("degPrevious");
    const nextButton = document.getElementById("degNext");
    const resetAllButton = document.getElementById("degResetAll");
    const exportButton = document.getElementById("degExportCsv");
    const loadingElement = document.getElementById("degLoading");
    const errorElement = document.getElementById("degError");
    const emptyElement = document.getElementById("degEmpty");

    // 文字欄位：使用包含文字的方式搜尋
    const textColumns = new Set([
        "GeneID",
        "GeneSymbol",
        "GeneName"
    ]);

    // 數值欄位：可以設定最小值、最大值
    const numericColumns = {
        baseMean: ["BaseMeanMin", "BaseMeanMax"],
        log2FoldChange: ["Log2FCMin", "Log2FCMax"],
        lfcSE: ["LfcSEMin", "LfcSEMax"],
        stat: ["StatMin", "StatMax"],
        pvalue: ["PValueMin", "PValueMax"],
        padj: ["PadjMin", "PadjMax"]
    };

    // 使用者目前設定的所有條件
    const filters = {};

    let headers = [];
    let currentPage = 1;
    let totalPages = 0;
    let sortColumn = "padj";
    let sortDirection = "asc";
    let activeColumn = null;
    let currentRequest = null;

    // 建立 HTML 元素
    function createElement(tag, className, text) {
        const element = document.createElement(tag);

        if (className) {
            element.className = className;
        }

        if (text !== undefined) {
            element.textContent = text;
        }

        return element;
    }

    function showError(message) {
        errorElement.textContent = message;
        errorElement.classList.remove("d-none");
    }

    function clearError() {
        errorElement.textContent = "";
        errorElement.classList.add("d-none");
    }

    function hasFilter(column) {
        if (textColumns.has(column)) {
            return Boolean(filters[column]);
        }

        const names = numericColumns[column];

        if (!names) return false;

        return Boolean(filters[names[0]] || filters[names[1]]);
    }

    // 產生表格標題與篩選按鈕
    function renderHeaders() {
        const tr = document.createElement("tr");

        headers.forEach(column => {
            const th = document.createElement("th");
            th.scope = "col";

            const wrapper = createElement(
                "div",
                "d-flex align-items-center gap-1"
            );

            const title = createElement(
                "span",
                "text-nowrap",
                column
            );

            const button = createElement(
                "button",
                hasFilter(column)
                    ? "btn btn-primary btn-sm"
                    : "btn btn-outline-secondary btn-sm",
                "⚙"
            );

            button.type = "button";
            button.title = `設定 ${column} 篩選與排序`;
            button.setAttribute(
                "aria-label",
                `設定 ${column} 篩選與排序`
            );

            button.addEventListener("click", () => {
                openFilterPanel(column);
            });

            wrapper.append(title, button);
            th.appendChild(wrapper);
            tr.appendChild(th);
        });

        tableHead.replaceChildren(tr);
    }

    // 動態建立輸入框
    function createInput(labelText, value, type = "text") {
        const group = createElement("div", "mb-3");
        const label = createElement(
            "label",
            "form-label",
            labelText
        );

        const input = createElement(
            "input",
            "form-control form-control-sm"
        );

        input.type = type;
        input.value = value ?? "";

        if (type === "number") {
            input.step = "any";
            input.placeholder = "不限制";
        } else {
            input.placeholder = `搜尋 ${labelText}`;
        }

        group.append(label, input);

        return { group, input };
    }

    // 開啟目前欄位的篩選面板
    function openFilterPanel(column) {
        activeColumn = column;
        filterPanel.replaceChildren();
        filterPanel.classList.remove("d-none");

        const top = createElement(
            "div",
            "d-flex justify-content-between align-items-center mb-3"
        );

        const title = createElement(
            "h6",
            "mb-0",
            `${column} 欄位設定`
        );

        const closeButton = createElement(
            "button",
            "btn-close"
        );

        closeButton.type = "button";
        closeButton.setAttribute(
            "aria-label",
            "關閉篩選面板"
        );

        closeButton.addEventListener("click", closeFilterPanel);

        top.append(title, closeButton);
        filterPanel.appendChild(top);

        const form = document.createElement("form");

        let inputs = [];

        if (textColumns.has(column)) {
            const field = createInput(
                column,
                filters[column] ?? ""
            );

            form.appendChild(field.group);
            inputs.push(field.input);
        }
        else if (numericColumns[column]) {
            const [minName, maxName] = numericColumns[column];

            const minField = createInput(
                "最小值",
                filters[minName] ?? "",
                "number"
            );

            const maxField = createInput(
                "最大值",
                filters[maxName] ?? "",
                "number"
            );

            const grid = createElement("div", "row g-2");

            [minField, maxField].forEach(field => {
                const col = createElement(
                    "div",
                    "col-md-6"
                );

                col.appendChild(field.group);
                grid.appendChild(col);
            });

            form.appendChild(grid);

            inputs.push(minField.input, maxField.input);
        }

        const actionRow = createElement(
            "div",
            "d-flex flex-wrap gap-2 mb-3"
        );

        const applyButton = createElement(
            "button",
            "btn btn-primary btn-sm",
            "套用條件"
        );

        applyButton.type = "submit";

        const clearButton = createElement(
            "button",
            "btn btn-outline-secondary btn-sm",
            "清除此欄條件"
        );

        clearButton.type = "button";

        clearButton.addEventListener("click", () => {
            if (textColumns.has(column)) {
                delete filters[column];
            } else {
                const [minName, maxName] = numericColumns[column];
                delete filters[minName];
                delete filters[maxName];
            }

            closeFilterPanel();
            currentPage = 1;
            renderHeaders();
            loadDegResults();
        });

        actionRow.append(applyButton, clearButton);

        form.appendChild(actionRow);

        // 套用目前欄位的篩選條件
        form.addEventListener("submit", event => {
            event.preventDefault();
            clearError();

            if (textColumns.has(column)) {
                const value = inputs[0].value.trim();

                if (value) {
                    filters[column] = value;
                } else {
                    delete filters[column];
                }
            }
            else {
                const [minName, maxName] = numericColumns[column];

                const minValue = inputs[0].value.trim();
                const maxValue = inputs[1].value.trim();

                if (
                    (minValue && !Number.isFinite(Number(minValue))) ||
                    (maxValue && !Number.isFinite(Number(maxValue))) ||
                    (minValue && maxValue &&
                        Number(minValue) > Number(maxValue))
                ) {
                    showError("數值範圍不正確，請確認最小值與最大值。");
                    return;
                }

                if (minValue) {
                    filters[minName] = minValue;
                } else {
                    delete filters[minName];
                }

                if (maxValue) {
                    filters[maxName] = maxValue;
                } else {
                    delete filters[maxName];
                }
            }

            currentPage = 1;
            closeFilterPanel();
            renderHeaders();
            loadDegResults();
        });

        filterPanel.appendChild(form);

        // 排序功能
        const sortTitle = createElement(
            "div",
            "fw-semibold mb-2",
            "排序方式"
        );

        const sortRow = createElement(
            "div",
            "d-flex flex-wrap gap-2"
        );

        [
            { label: "升冪 ↑", direction: "asc" },
            { label: "降冪 ↓", direction: "desc" }
        ].forEach(option => {
            const isActive =
                sortColumn === column &&
                sortDirection === option.direction;

            const button = createElement(
                "button",
                isActive
                    ? "btn btn-primary btn-sm"
                    : "btn btn-outline-primary btn-sm",
                option.label
            );

            button.type = "button";

            button.addEventListener("click", () => {
                sortColumn = column;
                sortDirection = option.direction;
                currentPage = 1;

                closeFilterPanel();
                loadDegResults();
            });

            sortRow.appendChild(button);
        });

        filterPanel.append(sortTitle, sortRow);
    }

    function closeFilterPanel() {
        activeColumn = null;
        filterPanel.classList.add("d-none");
        filterPanel.replaceChildren();
    }

    // 利用 textContent 安全顯示 CSV 資料
    function renderRows(rows) {
        const fragment = document.createDocumentFragment();

        rows.forEach(values => {
            const tr = document.createElement("tr");

            values.forEach(value => {
                const td = document.createElement("td");

                td.textContent = value ?? "";
                tr.appendChild(td);
            });

            fragment.appendChild(tr);
        });

        tableBody.replaceChildren(fragment);

        emptyElement.classList.toggle(
            "d-none",
            rows.length > 0
        );
    }

    // 從 ASP.NET Core API 取得完整篩選後的分頁資料
    async function loadDegResults() {
        // 取消上一個尚未完成的請求
        if (currentRequest) {
            currentRequest.abort();
        }

        const controller = new AbortController();
        currentRequest = controller;

        clearError();
        loadingElement.classList.remove("d-none");

        const url = new URL(apiUrl, window.location.href);

        url.searchParams.set("id", jobId);
        url.searchParams.set("Page", currentPage);
        url.searchParams.set("PageSize", pageSizeElement.value);
        url.searchParams.set("SortColumn", sortColumn);
        url.searchParams.set("SortDirection", sortDirection);

        Object.entries(filters).forEach(([key, value]) => {
            if (value !== "" && value != null) {
                url.searchParams.set(key, value);
            }
        });

        try {
            const response = await fetch(url.toString(), {
                method: "GET",
                credentials: "same-origin",
                signal: controller.signal,
                headers: {
                    "Accept": "application/json"
                }
            });

            if (!response.ok) {
                throw new Error(
                    `DEG 查詢失敗（HTTP ${response.status}）`
                );
            }

            const data = await response.json();

            // 忽略已被取消或過期的請求
            if (currentRequest !== controller) return;

            const incomingHeaders = data.headers ?? [];
            const rows = data.rows ?? [];

            // API Header 改變時重新建立標題
            if (
                headers.length !== incomingHeaders.length ||
                headers.some((h, i) => h !== incomingHeaders[i])
            ) {
                headers = incomingHeaders;
                renderHeaders();
            }

            totalPages = data.totalPages ?? 0;
            currentPage = data.page ?? currentPage;

            totalCountElement.textContent =
                (data.totalCount ?? 0).toLocaleString();

            renderRows(rows);

            const shownPage = totalPages === 0
                ? 0
                : currentPage;

            pageInfo.textContent =
                `第 ${shownPage} / ${totalPages} 頁`;

            previousButton.disabled = currentPage <= 1;
            nextButton.disabled =
                totalPages === 0 || currentPage >= totalPages;
        }
        catch (error) {
            if (error.name === "AbortError") return;
            if (currentRequest !== controller) return;

            showError(error.message || "無法取得 DEG 資料");
            tableBody.replaceChildren();
            totalCountElement.textContent = "--";
            pageInfo.textContent = "查詢失敗";
            previousButton.disabled = true;
            nextButton.disabled = true;
            emptyElement.classList.add("d-none");
        }
        finally {
            if (currentRequest === controller) {
                loadingElement.classList.add("d-none");
                currentRequest = null;
            }
        }
    }

    // 每頁筆數
    pageSizeElement.addEventListener("change", () => {
        currentPage = 1;
        loadDegResults();
    });

    // 上一頁
    previousButton.addEventListener("click", () => {
        if (currentPage > 1) {
            currentPage--;
            loadDegResults();
        }
    });

    // 下一頁
    nextButton.addEventListener("click", () => {
        if (currentPage < totalPages) {
            currentPage++;
            loadDegResults();
        }
    });

    // 清除所有條件
    resetAllButton.addEventListener("click", () => {
        Object.keys(filters).forEach(key => delete filters[key]);

        sortColumn = "padj";
        sortDirection = "asc";
        currentPage = 1;

        closeFilterPanel();
        renderHeaders();
        loadDegResults();
    });



    // 下載目前所有篩選條件下的完整 DEG CSV
    exportButton.addEventListener("click", async () => {

        clearError();

        const originalText = exportButton.textContent;
        exportButton.disabled = true;
        exportButton.textContent = "正在產生 CSV...";

        // 使用與 AJAX 表格相同的 API
        const url = new URL(apiUrl, window.location.href);

        url.searchParams.set("id", jobId);

        // 告訴 Controller：這次要下載 CSV，而非 JSON
        url.searchParams.set("exportCsv", "true");

        // 排序設定
        url.searchParams.set("SortColumn", sortColumn);
        url.searchParams.set("SortDirection", sortDirection);

        // 傳送目前全部欄位篩選條件
        Object.entries(filters).forEach(([key, value]) => {
            if (value !== "" && value != null) {
                url.searchParams.set(key, value);
            }
        });

        try {
            const response = await fetch(url.toString(), {
                method: "GET",
                credentials: "same-origin",
                headers: {
                    "Accept": "text/csv"
                }
            });

            if (!response.ok) {
                throw new Error(
                    `CSV 匯出失敗（HTTP ${response.status}）`
                );
            }

            // 將伺服器回傳的 CSV 轉為 Blob
            const blob = await response.blob();

            const downloadUrl = URL.createObjectURL(blob);

            // 建立暫時的下載連結
            const link = document.createElement("a");

            link.href = downloadUrl;
            link.download = `DEG_filtered_job_${jobId}.csv`;

            document.body.appendChild(link);
            link.click();
            link.remove();

            // 釋放暫時下載網址
            setTimeout(() => {
                URL.revokeObjectURL(downloadUrl);
            }, 1000);

        }
        catch (error) {
            showError(
                error.message || "無法下載篩選結果 CSV"
            );
        }
        finally {
            exportButton.disabled = false;
            exportButton.textContent = originalText;
        }
    });


    // 初始化 DEG 表格
    previousButton.disabled = true;
    nextButton.disabled = true;
    loadDegResults();
});
