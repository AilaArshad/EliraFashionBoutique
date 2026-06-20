/**
 * EliraFashionBoutique - Purchase Order Management Integration Javascript
 * 
 * This file orchestrates the frontend actions for:
 *   1. Left-Pane Supplier Master Table selections and highlights.
 *   2. Right-Pane Purchase Order Card renders and state changes.
 *   3. Collapsible Purchase Order Form (State Machine: Create vs Edit).
 *   4. Granular 7-Column Manifest Table Matrix with cascading dropdowns and real-time calculations.
 *   5. AJAX calls configured for C# ASP.NET Core endpoints.
 */

// Client-side API caching to prevent redundant AJAX queries and ensure seamless UI transitions
const apiCache = {
    subCategories: null,
    products: {},     // Key: subCategoryId
    variants: {},     // Key: productId
    variantDetails: {} // Key: variantId
};

let trackedActiveSupplierId = "1";
let rowCounter = 0;
let bsCollapse;

// Document Ready Initialization
window.addEventListener("DOMContentLoaded", async () => {
    // Initialize Bootstrap Collapse instance
    const formElement = document.getElementById('newOrderCollapseForm');
    if (formElement) {
        bsCollapse = new bootstrap.Collapse(formElement, { toggle: false });
    }

    // Load Initial Data
    try {
        await loadSuppliers();
        await syncAssociatedOrdersUI(trackedActiveSupplierId);
    } catch (error) {
        console.error("Initialization error:", error);
    }

    // Event listener for Master Create Button
    const createBtn = document.getElementById("toggleCollapseFormBtn");
    if (createBtn) {
        createBtn.addEventListener("click", () => {
            resetFormState();
            bsCollapse.toggle();
        });
    }

    // Event listener for Form Save Button
    const saveBtn = document.getElementById("savePurchaseOrderBtn");
    if (saveBtn) {
        saveBtn.addEventListener("click", submitPurchaseOrderForm);
    }

    // Event listener for Cancel Button
    const cancelBtn = document.getElementById("cancelFormBtn");
    if (cancelBtn) {
        cancelBtn.addEventListener("click", resetFormState);
    }

    // Event listener for Supplier Row Click Delegation
    const supplierTbody = document.getElementById("supplierTableBody");
    if (supplierTbody) {
        supplierTbody.addEventListener("click", async (e) => {
            const tr = e.target.closest("tr");
            if (!tr) return;
            const supplierId = tr.getAttribute("data-supplier-id");
            const supplierName = tr.querySelector(".fw-bold").innerText;

            document.getElementById("selectedSupplierTitleDisplay").innerText = supplierName;
            await syncAssociatedOrdersUI(supplierId);
            highlightSelectedSupplierRow(tr);
        });
    }

    // Event listener for adding new item rows
    const appendRowBtn = document.getElementById("appendVariantRowBtn");
    if (appendRowBtn) {
        appendRowBtn.addEventListener("click", handleAppendRowClick);
    }
});

// ==========================================
// PART 1: MASTER-DETAIL NAVIGATION PIPELINE
// ==========================================

/**
 * Loads the active suppliers list from database endpoints.
 */
async function loadSuppliers() {
    const response = await fetch("/api/suppliers");
    if (!response.ok) throw new Error("Failed to fetch suppliers.");
    const suppliers = await response.json();

    const tbody = document.getElementById("supplierTableBody");
    if (tbody) {
        tbody.innerHTML = "";
        suppliers.forEach(supplier => {
            const isActiveClass = supplier.id.toString() === trackedActiveSupplierId.toString() ? "active-row" : "";
            tbody.innerHTML += `
                <tr data-supplier-id="${supplier.id}" class="${isActiveClass}" style="cursor: pointer;">
                    <td>
                        <div class="fw-bold text-dark">${supplier.name}</div>
                        <small class="text-muted">ID Reference: ${supplier.id}</small>
                    </td>
                    <td><span class="badge bg-light text-dark border">${supplier.category}</span></td>
                </tr>`;
        });
    }

    // Populate Form Dropdown Select dynamically
    const formSupplierSelect = document.getElementById("formSupplierSelect");
    if (formSupplierSelect) {
        formSupplierSelect.innerHTML = suppliers.map(s => `
            <option value="${s.id}">${s.name}</option>
        `).join('');
    }
}

/**
 * Highlights the clicked supplier row visually.
 */
function highlightSelectedSupplierRow(selectedRow) {
    const tbody = document.getElementById("supplierTableBody");
    if (!tbody) return;
    tbody.querySelectorAll("tr").forEach(row => {
        row.classList.remove("active-row");
    });
    selectedRow.classList.add("active-row");
}

/**
 * Dynamically fetches and renders associated purchase orders for the selected supplier.
 */
async function syncAssociatedOrdersUI(supplierId) {
    trackedActiveSupplierId = supplierId.toString();
    const container = document.getElementById("inspectorOrdersContainer");
    if (!container) return;

    container.innerHTML = `<div class="text-center py-4 text-secondary small"><i class="fa-solid fa-spinner fa-spin me-2"></i>Loading records...</div>`;

    const response = await fetch(`/api/suppliers/${supplierId}/orders`);
    if (!response.ok) throw new Error("Failed to fetch supplier orders.");
    const orders = await response.json();

    container.innerHTML = "";

    if (orders.length === 0) {
        container.innerHTML = `<div class="text-center py-4 text-muted small bg-white border rounded">No orders found for this vendor.</div>`;
        return;
    }

    orders.forEach(po => {
        let badgeStyleClass = po.status === "Approved" ? "status-approved" : "status-pending";

        let nestedItemsHtml = po.items.map(item => {
            const skuName = item.variantDetails ? item.variantDetails.sku : `ID-${item.variantId}`;
            return `
                <div class="d-flex justify-content-between text-secondary border-bottom py-2" style="font-size: 0.85rem;">
                    <span>Variant SKU Reference: ${skuName} (x${item.quantityOrdered})</span>
                    <span class="font-monospace text-dark fw-medium">PKR ${item.subtotal.toLocaleString()}</span>
                </div>`;
        }).join('');

        container.innerHTML += `
            <div class="border rounded p-4 mb-3 bg-white shadow-sm">
                <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-2">
                    <div><span class="fw-bold text-dark" style="font-size: 0.95rem;">Order ID: #${po.purchaseOrderId}</span></div>
                    <div class="d-flex gap-2 align-items-center">
                        <span class="status-dropdown ${badgeStyleClass}">${po.status}</span>
                    </div>
                </div>
                <div class="py-1">${nestedItemsHtml}</div>
                <div class="d-flex justify-content-between align-items-center mt-3 pt-2 border-top">
                    <div>
                        <span class="text-muted small">Total Sum Amount:</span>
                        <h5 class="fw-bold text-primary m-0">PKR ${po.totalAmount.toLocaleString()}</h5>
                    </div>
                    <button class="btn btn-sm btn-outline-primary px-3 fw-medium" onclick="populateOrderToForm('${supplierId}', ${po.purchaseOrderId})">
                        <i class="fa-solid fa-pen-to-square me-1"></i> Update
                    </button>
                </div>
            </div>`;
    });
}

// ==========================================
// PART 2: THE FORM LIFE-CYCLE STATE MACHINE
// ==========================================

/**
 * Handles transitioning the form into EDIT mode and populating all saved inputs.
 */
async function populateOrderToForm(supplierId, orderId) {
    // Immediately show/expand the form container panel
    const formElement = document.getElementById('newOrderCollapseForm');
    if (formElement) {
        const bsInstance = bootstrap.Collapse.getOrCreateInstance(formElement);
        bsInstance.show();
    }

    // Immediately smooth scroll window up to the top form section
    window.scrollTo({ top: 0, behavior: 'smooth' });

    // Transition state titles and buttons
    document.getElementById("formPanelTitle").innerText = `EDIT PURCHASE ORDER RECORD: #${orderId}`;
    document.getElementById("editingOrderId").value = orderId;
    document.getElementById("savePurchaseOrderBtn").innerText = "Update Existing Order";

    // Set Metadata values
    document.getElementById("formSupplierSelect").value = supplierId;

    // Fetch order details
    const response = await fetch(`/api/orders/${orderId}`);
    if (!response.ok) throw new Error("Failed to fetch order details.");
    const targetOrder = await response.json();

    document.getElementById("formDeliveryDate").value = targetOrder.expectedDeliveryDate;
    document.getElementById("formOrderStatus").value = targetOrder.status;

    // Clean manifest table and load items
    const tbody = document.getElementById("draftManifestItemsBody");
    tbody.innerHTML = "";
    rowCounter = 0;

    for (const item of targetOrder.items) {
        rowCounter++;
        await appendVariantRowWithData(rowCounter, item);
    }
}

/**
 * Resets the form state back to CREATE MODE.
 */
function resetFormState() {
    const form = document.getElementById("purchaseOrderForm");
    if (form) form.reset();

    document.getElementById("editingOrderId").value = "";
    document.getElementById("formPanelTitle").innerText = "NEW PURCHASE ORDER DIALOG";
    document.getElementById("savePurchaseOrderBtn").innerText = "Commit & Save Order";
    document.getElementById("draftManifestItemsBody").innerHTML = "";

    // Defaults expected delivery date to 25/06/2026
    document.getElementById("formDeliveryDate").value = "2026-06-25";
    document.getElementById("formOrderStatus").value = "Pending Audit";

    evaluatePlaceholderState();

    if (bsCollapse) {
        bsCollapse.hide();
    }
}

// ==========================================
// PART 3: THE 7-COLUMN MANIFEST TABLE MATRIX
// ==========================================

/**
 * Appends a completely empty row to the draft manifest table (Create Mode addition).
 */
async function handleAppendRowClick() {
    rowCounter++;
    const tbody = document.getElementById("draftManifestItemsBody");

    // Clean out placeholder row
    const placeholder = document.getElementById("emptyRowPlaceholder");
    if (placeholder) placeholder.remove();

    const subcategories = await fetchSubCategories();
    const subCatOptions = subcategories.map(sc => `<option value="${sc.id}">${sc.name}</option>`).join('');

    const rowHtml = `
        <tr id="variantRow_${rowCounter}" class="manifest-item-row">
            <td><select class="form-select form-select-sm table-form-input subcat-select" onchange="handleSubCatChange(${rowCounter})">${subCatOptions}</select></td>
            <td><select class="form-select form-select-sm table-form-input product-select" onchange="handleProductChange(${rowCounter})"></select></td>
            <td><select class="form-select form-select-sm table-form-input variant-select" onchange="handleVariantChange(${rowCounter})"></select></td>
            <td><input type="number" class="form-control form-control-sm table-form-input qty-input" value="1" min="1" oninput="handleQtyChange(${rowCounter})"></td>
            <td><input type="text" class="form-control form-control-sm table-form-input cost-output" readonly disabled></td>
            <td><input type="text" class="form-control form-control-sm table-form-input font-monospace fw-bold subtotal-output" readonly disabled></td>
            <td class="text-center">
                <button type="button" class="btn btn-sm btn-link text-danger p-0 border-0" onclick="removeRow(${rowCounter})"><i class="fa-solid fa-circle-minus fa-lg"></i></button>
            </td>
        </tr>`;

    tbody.insertAdjacentHTML('beforeend', rowHtml);
    await handleSubCatChange(rowCounter);
}

/**
 * Appends a row populated with database item details (Edit Mode/Pre-population).
 */
async function appendVariantRowWithData(id, orderItem) {
    const tbody = document.getElementById("draftManifestItemsBody");

    const variantId = orderItem.variantId;
    const targetVariant = await fetchVariantDetails(variantId);

    const responseProd = await fetch(`/api/products/${targetVariant.productId}`);
    if (!responseProd.ok) throw new Error("Failed to fetch product details");
    const targetProduct = await responseProd.json();

    const targetSubCatId = targetProduct.subCategoryId;
    const subcategories = await fetchSubCategories();
    const subCatOptions = subcategories.map(sc => `<option value="${sc.id}" ${sc.id === targetSubCatId ? 'selected' : ''}>${sc.name}</option>`).join('');

    const rowHtml = `
        <tr id="variantRow_${id}" class="manifest-item-row">
            <td><select class="form-select form-select-sm table-form-input subcat-select" onchange="handleSubCatChange(${id})">${subCatOptions}</select></td>
            <td><select class="form-select form-select-sm table-form-input product-select" onchange="handleProductChange(${id})"></select></td>
            <td><select class="form-select form-select-sm table-form-input variant-select" onchange="handleVariantChange(${id})"></select></td>
            <td><input type="number" class="form-control form-control-sm table-form-input qty-input" value="${orderItem.quantityOrdered}" min="1" oninput="handleQtyChange(${id})"></td>
            <td><input type="text" class="form-control form-control-sm table-form-input cost-output" readonly disabled></td>
            <td><input type="text" class="form-control form-control-sm table-form-input font-monospace fw-bold subtotal-output" readonly disabled></td>
            <td class="text-center">
                <button type="button" class="btn btn-sm btn-link text-danger p-0 border-0" onclick="removeRow(${id})"><i class="fa-solid fa-circle-minus fa-lg"></i></button>
            </td>
        </tr>`;

    tbody.insertAdjacentHTML('beforeend', rowHtml);

    const row = document.getElementById(`variantRow_${id}`);

    const products = await fetchProductsBySubcategory(targetSubCatId);
    const productSelect = row.querySelector(".product-select");
    productSelect.innerHTML = products.map(p => `<option value="${p.id}">${p.name}</option>`).join('');
    productSelect.value = targetProduct.id;

    const variants = await fetchVariantsByProduct(targetProduct.id);
    const variantSelect = row.querySelector(".variant-select");
    variantSelect.innerHTML = variants.map(v => `<option value="${v.variantId}">${v.sku} (${v.size} - ${v.color})</option>`).join('');
    variantSelect.value = targetVariant.variantId;

    row.querySelector(".cost-output").value = targetVariant.price;
    handleQtyChange(id);
}

/**
 * Column 1 Dropdown Handler: Triggers cascade to Column 2 (Product Name).
 */
async function handleSubCatChange(rowId) {
    const row = document.getElementById(`variantRow_${rowId}`);
    if (!row) return;

    const subCatId = parseInt(row.querySelector(".subcat-select").value);
    const productSelect = row.querySelector(".product-select");

    const products = await fetchProductsBySubcategory(subCatId);
    productSelect.innerHTML = products.map(p => `<option value="${p.id}">${p.name}</option>`).join('');

    await handleProductChange(rowId);
}

/**
 * Column 2 Dropdown Handler: Triggers cascade to Column 3 (SKU Lookup).
 */
async function handleProductChange(rowId) {
    const row = document.getElementById(`variantRow_${rowId}`);
    if (!row) return;

    const productSelect = row.querySelector(".product-select");
    const variantSelect = row.querySelector(".variant-select");

    if (!productSelect.value) {
        variantSelect.innerHTML = "";
        return;
    }

    const productId = parseInt(productSelect.value);
    const variants = await fetchVariantsByProduct(productId);

    variantSelect.innerHTML = variants.map(v => `<option value="${v.variantId}">${v.sku} (${v.size} - ${v.color})</option>`).join('');

    await handleVariantChange(rowId);
}

/**
 * Column 3 Dropdown Handler: Pulls unit cost directly from DB variant details.
 */
async function handleVariantChange(rowId) {
    const row = document.getElementById(`variantRow_${rowId}`);
    if (!row) return;

    const variantSelect = row.querySelector(".variant-select");
    if (!variantSelect.value) return;

    const variantId = parseInt(variantSelect.value);
    const variantDetails = await fetchVariantDetails(variantId);

    if (variantDetails) {
        row.querySelector(".cost-output").value = variantDetails.price;
        handleQtyChange(rowId);
    }
}

/**
 * Column 4 (Qty) Change Handler: Real-time math multiplier.
 */
function handleQtyChange(rowId) {
    const row = document.getElementById(`variantRow_${rowId}`);
    if (!row) return;

    const qtyInput = row.querySelector(".qty-input");
    const costInput = row.querySelector(".cost-output");
    const subtotalInput = row.querySelector(".subtotal-output");

    let qty = parseInt(qtyInput.value) || 1;
    if (qty < 1) {
        qty = 1;
        qtyInput.value = 1;
    }

    const cost = parseFloat(costInput.value) || 0;
    const computedValue = qty * cost;

    subtotalInput.value = `PKR ${computedValue.toLocaleString()}`;
    subtotalInput.setAttribute("data-raw", computedValue);
}

/**
 * Column 7 Trash button click handler. Purges row from DOM.
 */
function removeRow(rowId) {
    const row = document.getElementById(`variantRow_${rowId}`);
    if (row) {
        row.remove();
    }
    evaluatePlaceholderState();
}

/**
 * Evaluates table row presence. If 0 items remain, restores warning placeholder row.
 */
function evaluatePlaceholderState() {
    const tbody = document.getElementById("draftManifestItemsBody");
    if (!tbody) return;

    const activeRows = tbody.querySelectorAll("tr:not(#emptyRowPlaceholder)");
    if (activeRows.length === 0) {
        tbody.innerHTML = `
            <tr id="emptyRowPlaceholder">
                <td colspan="7" class="text-center text-muted py-4">No items added to this purchase draft yet. Click the button below to append rows.</td>
            </tr>`;
    }
}

// ==========================================
// PART 4: SUBMISSION & INVENTORY CONTROLS
// ==========================================

/**
 * Handles Form Submission payload creation and endpoint routing.
 */
async function submitPurchaseOrderForm() {
    const chosenSupplier = document.getElementById("formSupplierSelect").value;
    const deliveryDate = document.getElementById("formDeliveryDate").value;
    const orderStatus = document.getElementById("formOrderStatus").value;
    const editingId = document.getElementById("editingOrderId").value;

    const rows = document.querySelectorAll(".manifest-item-row");

    if (rows.length === 0) {
        alert("Manifest is empty! Please append items before saving.");
        return;
    }

    // Execute frontend validation to ensure no row in the manifest table has empty dropdowns or missing values
    let isValid = true;
    rows.forEach(row => {
        const subcatSelect = row.querySelector(".subcat-select");
        const productSelect = row.querySelector(".product-select");
        const variantSelect = row.querySelector(".variant-select");
        const qtyInput = row.querySelector(".qty-input");

        const subcatVal = subcatSelect ? subcatSelect.value.trim() : "";
        const productVal = productSelect ? productSelect.value.trim() : "";
        const variantVal = variantSelect ? variantSelect.value.trim() : "";
        const qtyVal = qtyInput ? qtyInput.value.trim() : "";

        if (!subcatVal || !productVal || !variantVal || !qtyVal || parseInt(qtyVal) < 1) {
            isValid = false;
        }
    });

    if (!isValid) {
        alert("Please ensure all rows in the manifest table have a selected Category, Product, Variant, and a valid Quantity.");
        return;
    }

    let url = "/api/orders";
    let parentPayload;

    if (editingId) {
        url = "/api/purchaseorders/update";
        const itemsPayload = [];
        rows.forEach(row => {
            const variantId = parseInt(row.querySelector(".variant-select").value);
            const qty = parseInt(row.querySelector(".qty-input").value);
            const unitCost = parseFloat(row.querySelector(".cost-output").value) || 0;

            itemsPayload.push({
                variantId: variantId,
                qty: qty,
                unitCost: unitCost
            });
        });

        parentPayload = {
            purchaseOrderId: parseInt(editingId),
            supplierId: parseInt(chosenSupplier),
            expectedDeliveryDate: deliveryDate,
            status: orderStatus,
            manifestItems: itemsPayload
        };
    } else {
        const itemsPayload = [];
        rows.forEach(row => {
            const variantId = parseInt(row.querySelector(".variant-select").value);
            const qty = parseInt(row.querySelector(".qty-input").value);
            const subtotal = parseFloat(row.querySelector(".subtotal-output").getAttribute("data-raw")) || 0;

            itemsPayload.push({
                variantId: variantId,
                quantityOrdered: qty,
                subtotal: subtotal
            });
        });

        parentPayload = {
            supplierId: parseInt(chosenSupplier),
            expectedDeliveryDate: deliveryDate,
            status: orderStatus,
            items: itemsPayload
        };
    }

    try {
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(parentPayload)
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.detail || "Database write transaction failed.");
        }

        alert("Purchase order transaction saved successfully!");
        resetFormState();
        await syncAssociatedOrdersUI(chosenSupplier);

    } catch (error) {
        alert(`Error saving order: ${error.message}`);
        console.error("Submission error:", error);
    }
}

// ==========================================
// DATA FETCHING WRAPPERS
// ==========================================

async function fetchSubCategories() {
    if (apiCache.subCategories) return apiCache.subCategories;
    const response = await fetch("/api/subcategories");
    if (!response.ok) throw new Error("Subcategories fetch failure.");
    const data = await response.json();
    apiCache.subCategories = data;
    return data;
}

async function fetchProductsBySubcategory(subCategoryId) {
    if (apiCache.products[subCategoryId]) return apiCache.products[subCategoryId];
    const response = await fetch(`/api/products/by-subcategory?subcategory_id=${subCategoryId}`);
    if (!response.ok) throw new Error("Products fetch failure.");
    const data = await response.json();
    apiCache.products[subCategoryId] = data;
    return data;
}

async function fetchVariantsByProduct(productId) {
    if (apiCache.variants[productId]) return apiCache.variants[productId];
    const response = await fetch(`/api/variants/by-product?product_id=${productId}`);
    if (!response.ok) throw new Error("Variants fetch failure.");
    const data = await response.json();
    apiCache.variants[productId] = data;
    return data;
}

async function fetchVariantDetails(variantId) {
    if (apiCache.variantDetails[variantId]) return apiCache.variantDetails[variantId];
    const response = await fetch(`/api/variants/${variantId}`);
    if (!response.ok) throw new Error("Variant detail fetch failure.");
    const data = await response.json();
    apiCache.variantDetails[variantId] = data;
    return data;
}

// Expose event handlers globally to bind with the user's pre-built HTML inline calls
window.handleSubCatChange = handleSubCatChange;
window.handleProductChange = handleProductChange;
window.handleVariantChange = handleVariantChange;
window.handleQtyChange = handleQtyChange;
window.removeRow = removeRow;
window.populateOrderToForm = populateOrderToForm;
window.evaluatePlaceholderState = evaluatePlaceholderState;
window.resetFormState = resetFormState;
window.syncAssociatedOrdersUI = syncAssociatedOrdersUI;
