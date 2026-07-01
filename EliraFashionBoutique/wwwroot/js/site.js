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
    categories: null,
    suppliers: null,
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

    // Immediately clear mock data in the supplier dropdown
    const formSupplierSelect = document.getElementById("formSupplierSelect");
    if (formSupplierSelect) {
        formSupplierSelect.innerHTML = "";
    }

    // Add event listener to formCategorySelect
    const formCategorySelect = document.getElementById("formCategorySelect");
    if (formCategorySelect) {
        formCategorySelect.addEventListener("change", handleTopCategoryChange);
    }

    // Load Initial Data
    if (formElement && formSupplierSelect && formCategorySelect) {
        try {
            await loadCategories();
            await loadSuppliers();
            await handleTopCategoryChange();
            await syncAssociatedOrdersUI(trackedActiveSupplierId);
        } catch (error) {
            console.error("Initialization error:", error);
        }
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
    const suppliers = await fetchSuppliers();

    const tbody = document.getElementById("supplierTableBody");
    if (tbody) {
        tbody.innerHTML = "";

        // Filter suppliers to only show those with purchase orders (real-time linking details)
        const activeSuppliers = suppliers.filter(s => s.hasOrders);
        activeSuppliers.forEach(supplier => {
            const isActiveClass = supplier.id.toString() === trackedActiveSupplierId.toString() ? "active-row" : "";
            tbody.innerHTML += `
                <tr data-supplier-id="${supplier.id}" class="${isActiveClass}" style="cursor: pointer;">
                    <td>
                        <div class="fw-bold text-dark">${supplier.name}</div>
                        <small class="text-muted">ID Reference: ${supplier.id}</small>
                    </td>
                    <td><span class="badge bg-light text-dark border">${supplier.category || ""}</span></td>
                </tr>`;
        });
    }
}

async function fetchCategories() {
    if (apiCache.categories) return apiCache.categories;
    const response = await fetch("/api/categories");
    if (!response.ok) throw new Error("Failed to fetch categories.");
    const data = await response.json();
    apiCache.categories = data;
    return data;
}

async function fetchSuppliers() {
    const response = await fetch("/api/suppliers");
    if (!response.ok) throw new Error("Failed to fetch suppliers.");
    const data = await response.json();
    return data;
}

async function loadCategories() {
    const categories = await fetchCategories();
    const formCategorySelect = document.getElementById("formCategorySelect");
    if (formCategorySelect) {
        formCategorySelect.innerHTML = categories.map(c => `
            <option value="${c.id}">${c.name}</option>
        `).join('');
    }
}

async function handleTopCategoryChange() {
    const categorySelect = document.getElementById("formCategorySelect");
    if (!categorySelect) return;
    const selectedCategoryId = parseInt(categorySelect.value);

    // 1. Ensure the "Select Supplier" combobox retains or refetches all active suppliers from the database
    const allSuppliers = await fetchSuppliers();
    const formSupplierSelect = document.getElementById("formSupplierSelect");
    if (formSupplierSelect) {
        const prevValue = formSupplierSelect.value;
        formSupplierSelect.innerHTML = allSuppliers.map(s => `
            <option value="${s.id}">${s.name}</option>
        `).join('');
        if (prevValue) {
            formSupplierSelect.value = prevValue;
        }
    }

    // 2. Filter row-level sub-categories of any rows currently in the manifest table
    const rows = document.querySelectorAll(".manifest-item-row");
    for (const row of rows) {
        const subcatSelect = row.querySelector(".subcat-select");
        const subcategories = await fetchSubCategories();
        const filteredSubCats = subcategories.filter(sc => sc.categoryId === selectedCategoryId);

        subcatSelect.innerHTML = filteredSubCats.map(sc => `<option value="${sc.id}">${sc.name}</option>`).join('');

        const rowId = parseInt(row.id.replace("variantRow_", ""));
        await handleSubCatChange(rowId);
    }
}

async function syncSuppliersForCategory(supplierIdToSelect) {
    const allSuppliers = await fetchSuppliers();
    const formSupplierSelect = document.getElementById("formSupplierSelect");
    if (formSupplierSelect) {
        formSupplierSelect.innerHTML = allSuppliers.map(s => `
            <option value="${s.id}">${s.name}</option>
        `).join('');
        if (supplierIdToSelect) {
            formSupplierSelect.value = supplierIdToSelect;
        }
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
                    ${po.status === "Approved" ? "" : `
                    <button class="btn btn-sm btn-outline-primary px-3 fw-medium" onclick="populateOrderToForm('${supplierId}', ${po.purchaseOrderId})">
                        <i class="fa-solid fa-pen-to-square me-1"></i> Update
                    </button>
                    `}
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

    // Fetch order details
    const response = await fetch(`/api/orders/${orderId}`);
    if (!response.ok) throw new Error("Failed to fetch order details.");
    const targetOrder = await response.json();

    // Dynamically identify and set the category belonging to this order's items
    if (targetOrder.items && targetOrder.items.length > 0) {
        const firstItem = targetOrder.items[0];
        const targetVariant = await fetchVariantDetails(firstItem.variantId);
        const responseProd = await fetch(`/api/products/${targetVariant.productId}`);
        if (responseProd.ok) {
            const targetProduct = await responseProd.json();
            const subcategories = await fetchSubCategories();
            const subcat = subcategories.find(sc => sc.id === targetProduct.subCategoryId);
            if (subcat && subcat.categoryId) {
                const categorySelect = document.getElementById("formCategorySelect");
                if (categorySelect) {
                    categorySelect.value = subcat.categoryId;
                }
            }
        }
    }

    // Sync supplier dropdown based on the identified category, and select the order's supplier
    await syncSuppliersForCategory(supplierId);

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

    // Apply the locking logic based on order status (exactly "Approved")
    const isApproved = targetOrder.status && targetOrder.status.trim().toLowerCase() === "approved";
    setFormLockState(isApproved);
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

    // Reset Category dropdown selection and refresh suppliers list matching first category
    const categorySelect = document.getElementById("formCategorySelect");
    if (categorySelect) {
        categorySelect.selectedIndex = 0;
        handleTopCategoryChange();
    }

    const formElement = document.getElementById('newOrderCollapseForm');
    if (formElement) {
        bootstrap.Collapse.getOrCreateInstance(formElement).hide();
    }

    setFormLockState(false);
}

/**
 * Lock/Unlock form inputs, dropdowns, and save buttons based on read-only requirement.
 */
function setFormLockState(isLocked) {
    const saveBtn = document.getElementById("savePurchaseOrderBtn");
    if (saveBtn) {
        if (isLocked) {
            saveBtn.style.display = "none";
        } else {
            saveBtn.style.display = "";
        }
        saveBtn.disabled = false;
    }

    const supplierSelect = document.getElementById("formSupplierSelect");
    if (supplierSelect) {
        supplierSelect.disabled = false;
    }

    const categorySelect = document.getElementById("formCategorySelect");
    if (categorySelect) {
        categorySelect.disabled = false;
    }

    const deliveryDate = document.getElementById("formDeliveryDate");
    if (deliveryDate) {
        deliveryDate.disabled = false;
        deliveryDate.readOnly = false;
    }

    const orderStatus = document.getElementById("formOrderStatus");
    if (orderStatus) {
        orderStatus.disabled = false;
    }

    const appendBtn = document.getElementById("appendVariantRowBtn");
    if (appendBtn) {
        appendBtn.disabled = false;
    }

    // Lock/Unlock existing rows in manifest table
    const rows = document.querySelectorAll(".manifest-item-row");
    rows.forEach(row => {
        const subcat = row.querySelector(".subcat-select");
        if (subcat) subcat.disabled = false;

        const product = row.querySelector(".product-select");
        if (product) product.disabled = false;

        const variant = row.querySelector(".variant-select");
        if (variant) variant.disabled = false;

        const qty = row.querySelector(".qty-input");
        if (qty) {
            qty.disabled = false;
            qty.readOnly = false;
        }

        const trashBtn = row.querySelector("button");
        if (trashBtn) {
            trashBtn.disabled = false;
            trashBtn.style.pointerEvents = "auto";
            trashBtn.classList.add("text-danger");
            trashBtn.classList.remove("text-secondary");
        }
    });
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
    const selectedCategoryId = parseInt(document.getElementById("formCategorySelect").value);
    const filteredSubcategories = subcategories.filter(sc => sc.categoryId === selectedCategoryId);
    const subCatOptions = filteredSubcategories.map(sc => `<option value="${sc.id}">${sc.name}</option>`).join('');

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
    const selectedCategoryId = parseInt(document.getElementById("formCategorySelect").value);
    const filteredSubcategories = subcategories.filter(sc => sc.categoryId === selectedCategoryId);
    const subCatOptions = filteredSubcategories.map(sc => `<option value="${sc.id}" ${sc.id === targetSubCatId ? 'selected' : ''}>${sc.name}</option>`).join('');

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
    variantSelect.innerHTML = variants.map(v => `<option value="${v.variantId}">${v.sku}(${v.size}-${v.color}) ${v.price}</option>`).join('');
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

    variantSelect.innerHTML = variants.map(v => `<option value="${v.variantId}">${v.sku}(${v.size}-${v.color}) ${v.price}</option>`).join('');

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
        showToast('Manifest is empty! Please append at least one item before saving.', 'warning');
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
        showToast('Please ensure all rows have a selected Category, Product, Variant, and a valid Quantity.', 'warning');
        return;
    }

    let url = "/api/orders";
    let parentPayload;

    if (editingId) {
        url = "/api/purchaseorders/update";
        const itemsPayload = [];
        rows.forEach(row => {
            const variantId = parseInt(row.querySelector(".variant-select").value);
            const quantityOrdered = parseInt(row.querySelector(".qty-input").value);
            const unitCost = parseFloat(row.querySelector(".cost-output").value) || 0;

            itemsPayload.push({
                variantId: variantId,
                quantityOrdered: quantityOrdered,
                unitCost: unitCost
            });
        });

        parentPayload = {
            purchaseOrderId: parseInt(editingId),
            supplierId: parseInt(chosenSupplier),
            expectedDeliveryDate: deliveryDate,
            status: orderStatus,
            purchaseOrderItems: itemsPayload
        };
    } else {
        const itemsPayload = [];
        rows.forEach(row => {
            const variantId = parseInt(row.querySelector(".variant-select").value);
            const quantityOrdered = parseInt(row.querySelector(".qty-input").value);
            const subtotal = parseFloat(row.querySelector(".subtotal-output").getAttribute("data-raw")) || 0;

            itemsPayload.push({
                variantId: variantId,
                quantityOrdered: quantityOrdered,
                subtotal: subtotal
            });
        });

        parentPayload = {
            supplierId: parseInt(chosenSupplier),
            expectedDeliveryDate: deliveryDate,
            status: orderStatus,
            purchaseOrderItems: itemsPayload
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

        showToast('Purchase order committed and saved successfully!', 'success');
        resetFormState();

        // Clear client-side cache so fresh lists are loaded in real-time
        apiCache.suppliers = null;
        apiCache.categories = null;

        await loadSuppliers();
        await handleTopCategoryChange();
        await syncAssociatedOrdersUI(chosenSupplier);

    } catch (error) {
        showToast(`Error saving order: ${error.message}`, 'error');
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
window.handleTopCategoryChange = handleTopCategoryChange;

// ==========================================
// PART 5: DYNAMIC INVENTORY STOCK HUB SYSTEM
// ==========================================
let targetedInventoryProductId = null;

async function renderInventoryCatalogTable() {
    const tbody = document.getElementById("productCatalogStockBody");
    if (!tbody) return;
    tbody.innerHTML = `<tr><td colspan="4" class="text-center py-4 text-secondary small"><i class="fa-solid fa-spinner fa-spin me-2"></i>Loading inventory matrix...</td></tr>`;

    try {
        const response = await fetch("/api/inventory");
        if (!response.ok) throw new Error("Failed to fetch inventory.");
        const data = await response.json();

        tbody.innerHTML = "";

        if (data.length === 0) {
            tbody.innerHTML = `<tr><td colspan="4" class="text-center py-4 text-muted small">No products in database.</td></tr>`;
            return;
        }

        if (targetedInventoryProductId === null) {
            targetedInventoryProductId = data[0].id;
        }

        data.forEach(prod => {
            let stateBadgeMarkup = `<span class="badge-pill-custom stock-healthy"><i class="fa-solid fa-circle-check me-1"></i> Stock Healthy</span>`;
            if (prod.triggerReorderWarning) {
                stateBadgeMarkup = `<span class="badge-pill-custom stock-reorder"><i class="fa-solid fa-triangle-exclamation me-1"></i> Needs Reorder</span>`;
            }
            if (prod.isOutOfStock) {
                stateBadgeMarkup = `<span class="badge-pill-custom stock-critical"><i class="fa-solid fa-circle-xmark me-1"></i> Out of Stock</span>`;
            }

            const isCurrentActiveRow = prod.id === targetedInventoryProductId ? "active-row" : "";

            tbody.innerHTML += `
                <tr data-product-id="${prod.id}" class="${isCurrentActiveRow}" style="cursor: pointer;">
                    <td>
                        <div class="fw-bold text-dark mb-0">${prod.name}</div>
                        <small class="text-muted d-block text-truncate" style="max-width: 220px;">${prod.description}</small>
                    </td>
                    <td><span class="font-monospace text-secondary bg-light px-2 py-1 rounded small border">${prod.sku}</span></td>
                    <td class="text-center fw-bold fs-6">${prod.cumulativeStock}</td>
                    <td>${stateBadgeMarkup}</td>
                </tr>`;
        });

        if (targetedInventoryProductId !== null) {
            syncVariantBreakdownPanel(targetedInventoryProductId);
        }
    } catch (error) {
        console.error("Error loading inventory:", error);
        tbody.innerHTML = `<tr><td colspan="4" class="text-center text-danger py-4 small">Error loading inventory: ${error.message}</td></tr>`;
    }
}

async function syncVariantBreakdownPanel(productId) {
    targetedInventoryProductId = productId;
    const container = document.getElementById("variantLiveContainer");
    if (!container) return;
    container.innerHTML = `<div class="text-center py-4 text-secondary small"><i class="fa-solid fa-spinner fa-spin me-2"></i>Loading variants breakdown...</div>`;

    try {
        const leftRow = document.querySelector(`tr[data-product-id="${productId}"]`);
        if (leftRow) {
            const nameDiv = leftRow.querySelector(".fw-bold");
            document.getElementById("selectedProductNameDisplay").innerText = nameDiv ? nameDiv.innerText : "---";
        }

        const response = await fetch(`/api/inventory/${productId}/variants`);
        if (!response.ok) throw new Error("Failed to fetch variant breakdown.");
        const structuralVariants = await response.json();

        container.innerHTML = "";

        if (structuralVariants.length === 0) {
            container.innerHTML = `<div class="text-center py-4 text-muted small">No variant metrics associated here.</div>`;
            return;
        }

        structuralVariants.forEach(variant => {
            const isUnderReorderBoundary = variant.quantityAvailable <= variant.reorderLevel;
            let badgeClass = "stock-healthy";
            let alertContextMarkup = "";

            if (isUnderReorderBoundary) {
                badgeClass = "stock-reorder";
                alertContextMarkup = `
                    <div class="alert alert-warning p-2 mt-2 mb-0 d-flex align-items-center gap-2" style="font-size: 0.72rem; border-radius: 6px;">
                        <i class="fa-solid fa-bell"></i>
                        <span>Stock is below safety boundary margin.</span>
                    </div>`;
            }
            if (variant.quantityAvailable === 0) badgeClass = "stock-critical";

            container.innerHTML += `
                <div class="border rounded p-3 bg-light-subtle shadow-sm position-relative" style="border-color: #f1f5f9;">
                    <div class="d-flex justify-content-between align-items-start gap-2 mb-2">
                        <div>
                            <span class="font-monospace fw-bold text-dark d-block mb-1" style="font-size: 0.85rem;">${variant.sku}</span>
                            <div class="d-flex align-items-center gap-2 text-secondary small">
                                <span>Size: <strong class="text-dark">${variant.size}</strong></span>
                                <span class="text-muted">|</span>
                                <span class="d-inline-flex align-items-center gap-1">
                                    Color: <span class="color-preview-dot" style="background-color: ${variant.hex};"></span> 
                                    <strong class="text-dark">${variant.colorName}</strong>
                                </span>
                            </div>
                        </div>
                        <div class="text-end">
                            <span class="badge-pill-custom ${badgeClass} py-1 px-2 mb-1" style="font-size: 0.68rem;">Qty: ${variant.quantityAvailable}</span>
                        </div>
                    </div>

                    <div class="row g-2 pt-2 border-top mt-2 align-items-center">
                        <div class="col-6 text-muted" style="font-size: 0.75rem;">
                            Reorder Threshold: <strong class="text-dark">${variant.reorderLevel} units</strong>
                        </div>
                        <div class="col-6 text-end">
                            <button type="button" class="btn-link-action" onclick="triggerThresholdUpdateControl('${variant.sku}', ${variant.reorderLevel})">
                                <i class="fa-solid fa-pen-to-square me-1"></i>Update Threshold
                            </button>
                        </div>
                    </div>
                    
                    <div class="text-muted mt-2" style="font-size: 0.68rem; font-style: italic;">
                        <i class="fa-solid fa-clock me-1"></i> System Timestamp: ${variant.lastUpdated}
                    </div>

                    ${alertContextMarkup}
                </div>`;
        });
    } catch (error) {
        console.error("Error loading variants:", error);
        container.innerHTML = `<div class="text-center py-4 text-danger small">Error loading breakdown: ${error.message}</div>`;
    }
}

function triggerThresholdUpdateControl(sku, currentLevel) {
    const formCollapseElement = document.getElementById('thresholdAdjustmentFormCollapse');
    const displayField = document.getElementById('adjustVariantSKUDisplay');
    const inputElement = document.getElementById('adjustThresholdInput');
    if (!formCollapseElement || !displayField || !inputElement) return;

    displayField.value = sku;
    inputElement.value = currentLevel;

    const bsCollapse = bootstrap.Collapse.getOrCreateInstance(formCollapseElement);
    bsCollapse.show();

    formCollapseElement.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    inputElement.focus();
}

window.renderInventoryCatalogTable = renderInventoryCatalogTable;
window.syncVariantBreakdownPanel = syncVariantBreakdownPanel;
window.triggerThresholdUpdateControl = triggerThresholdUpdateControl;

