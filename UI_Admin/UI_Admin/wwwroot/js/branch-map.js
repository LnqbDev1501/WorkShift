document.addEventListener('DOMContentLoaded', function () {
    const mapContainer = document.getElementById('branchMap');
    if (!mapContainer) return;

    const tileMapKey = window.vietMapConfig?.tileMapKey;

    if (!tileMapKey) {
        console.error("VietMap TileMapKey is missing.");
        mapContainer.innerHTML = '<div class="p-4 text-center text-error font-body-sm">VietMap TileMapKey is missing. Please configure VietMap:TileMapKey in user-secrets.</div>';
        return;
    }

    if (typeof vietmapgl === 'undefined') {
        console.error("VIETMAP GL JS SDK is not loaded.");
        return;
    }

    const latInput = document.getElementById('latitude') || document.getElementById('branch-latitude') || document.querySelector('input[name="Latitude"]');
    const lngInput = document.getElementById('longitude') || document.getElementById('branch-longitude') || document.querySelector('input[name="Longitude"]');
    const addressInput = document.getElementById('branchAddress') || document.getElementById('branch-address') || document.querySelector('textarea[name="Address"]');

    const searchInput = document.getElementById('searchLocationInput');
    const dropdown = document.getElementById('searchResultsDropdown');
    const clearSearchBtn = document.getElementById('clearSearchBtn');
    const searchContainer = document.getElementById('search-location-container');

    if (latInput) latInput.readOnly = true;
    if (lngInput) lngInput.readOnly = true;

    // Default location: Can Tho / Vietnam [longitude, latitude] -> VIETMAP GL JS uses [lng, lat]
    const defaultLng = 105.78;
    const defaultLat = 10.03;
    const defaultZoom = 13;
    const editZoom = 16;

    let rawLat = latInput ? parseFloat(latInput.value) : NaN;
    let rawLng = lngInput ? parseFloat(lngInput.value) : NaN;

    const hasValidCoords = !isNaN(rawLat) && !isNaN(rawLng) &&
                           rawLat >= -90 && rawLat <= 90 &&
                           rawLng >= -180 && rawLng <= 180;

    const center = hasValidCoords ? [rawLng, rawLat] : [defaultLng, defaultLat];
    const zoom = hasValidCoords ? editZoom : defaultZoom;

    let map = null;
    let marker = null;

    try {
        map = new vietmapgl.Map({
            container: "branchMap",
            style: `https://maps.vietmap.vn/maps/styles/tm/style.json?apikey=${tileMapKey}`,
            center: center,
            zoom: zoom
        });

        map.addControl(new vietmapgl.NavigationControl());

        if (hasValidCoords) {
            marker = new vietmapgl.Marker({ draggable: true })
                .setLngLat([rawLng, rawLat])
                .addTo(map);

            marker.on('dragend', handleMarkerDragEnd);
        }

        map.on('click', function (e) {
            const lng = e.lngLat.lng;
            const lat = e.lngLat.lat;

            updateMarkerAndCoords(lat, lng);
            reverseGeocode(lat, lng);
        });
    } catch (err) {
        console.error("Error initializing VIETMAP GL JS map:", err);
    }

    function handleMarkerDragEnd() {
        if (!marker) return;
        const pos = marker.getLngLat();
        updateMarkerAndCoords(pos.lat, pos.lng);
        reverseGeocode(pos.lat, pos.lng);
    }

    function updateMarkerAndCoords(lat, lng) {
        const formattedLat = parseFloat(lat.toFixed(6));
        const formattedLng = parseFloat(lng.toFixed(6));

        if (latInput) {
            latInput.value = formattedLat;
            latInput.dispatchEvent(new Event('input', { bubbles: true }));
            latInput.dispatchEvent(new Event('change', { bubbles: true }));
        }

        if (lngInput) {
            lngInput.value = formattedLng;
            lngInput.dispatchEvent(new Event('input', { bubbles: true }));
            lngInput.dispatchEvent(new Event('change', { bubbles: true }));
        }

        if (map) {
            if (marker) {
                marker.setLngLat([lng, lat]);
            } else {
                marker = new vietmapgl.Marker({ draggable: true })
                    .setLngLat([lng, lat])
                    .addTo(map);

                marker.on('dragend', handleMarkerDragEnd);
            }
        }
    }

    async function reverseGeocode(lat, lng) {
        try {
            const response = await fetch(`/Branches/ReverseLocation?latitude=${lat}&longitude=${lng}`);
            if (!response.ok) return;

            const data = await response.json();
            const newAddress = data?.address || data?.display;

            if (newAddress && addressInput) {
                addressInput.value = newAddress;
                addressInput.dispatchEvent(new Event('input', { bubbles: true }));
                addressInput.dispatchEvent(new Event('change', { bubbles: true }));

                if (searchInput && !searchInput.value.trim()) {
                    searchInput.value = newAddress;
                    if (clearSearchBtn) clearSearchBtn.classList.remove('hidden');
                }
            }
        } catch (err) {
            console.warn("Reverse geocoding failed or was aborted:", err);
        }
    }

    // --- AUTOCOMPLETE SEARCH LOGIC ---
    let debounceTimer = null;
    let searchAbortController = null;

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const query = this.value.trim();

            if (clearSearchBtn) {
                if (query.length > 0) {
                    clearSearchBtn.classList.remove('hidden');
                } else {
                    clearSearchBtn.classList.add('hidden');
                }
            }

            if (debounceTimer) clearTimeout(debounceTimer);

            if (query.length < 3) {
                hideDropdown();
                return;
            }

            debounceTimer = setTimeout(() => {
                fetchSuggestions(query);
            }, 400);
        });

        searchInput.addEventListener('focus', function () {
            if (dropdown && dropdown.children.length > 0 && this.value.trim().length >= 3) {
                dropdown.classList.remove('hidden');
            }
        });
    }

    if (clearSearchBtn) {
        clearSearchBtn.addEventListener('click', function () {
            if (searchInput) searchInput.value = '';
            clearSearchBtn.classList.add('hidden');
            hideDropdown();
        });
    }

    async function fetchSuggestions(query) {
        if (searchAbortController) {
            searchAbortController.abort();
        }

        searchAbortController = new AbortController();

        try {
            const response = await fetch(`/Branches/SearchLocation?text=${encodeURIComponent(query)}`, {
                signal: searchAbortController.signal
            });

            if (!response.ok) {
                hideDropdown();
                return;
            }

            const items = await response.json();
            renderDropdown(items);
        } catch (err) {
            if (err.name !== 'AbortError') {
                console.error("Autocomplete search error:", err);
                hideDropdown();
            }
        }
    }

    function renderDropdown(items) {
        if (!dropdown) return;
        dropdown.innerHTML = '';

        if (!Array.isArray(items) || items.length === 0) {
            const emptyMsg = document.createElement('div');
            emptyMsg.className = 'p-3 text-center text-on-surface-variant font-body-sm';
            emptyMsg.textContent = 'No matching locations found.';
            dropdown.appendChild(emptyMsg);
            dropdown.classList.remove('hidden');
            return;
        }

        items.slice(0, 8).forEach(item => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'suggestion-item flex flex-col gap-0.5 text-left w-full hover:bg-surface-container transition-colors';

            const titleSpan = document.createElement('span');
            titleSpan.className = 'font-body-md-medium text-on-surface truncate';
            titleSpan.textContent = item.name || item.display || 'Location';

            const addrSpan = document.createElement('span');
            addrSpan.className = 'font-body-sm text-on-surface-variant truncate';
            addrSpan.textContent = item.address || item.display || '';

            btn.appendChild(titleSpan);
            if (item.address && item.address !== item.name) {
                btn.appendChild(addrSpan);
            }

            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                selectSuggestion(item);
            });

            dropdown.appendChild(btn);
        });

        dropdown.classList.remove('hidden');
    }

    async function selectSuggestion(item) {
        hideDropdown();
        const refId = item.refId || item.ref_id;

        if (searchInput) {
            searchInput.value = item.name || item.display || item.address || '';
        }

        if (!refId) return;

        try {
            const response = await fetch(`/Branches/GetLocation?refId=${encodeURIComponent(refId)}`);
            if (!response.ok) return;

            const place = await response.json();
            const lat = place.latitude || place.lat;
            const lng = place.longitude || place.lng;

            if (typeof lat === 'number' && typeof lng === 'number' && !isNaN(lat) && !isNaN(lng)) {
                updateMarkerAndCoords(lat, lng);

                if (map) {
                    map.flyTo({
                        center: [lng, lat],
                        zoom: 16
                    });
                }

                const placeAddr = place.address || place.name || item.address || item.name;
                if (placeAddr && addressInput) {
                    addressInput.value = placeAddr;
                    addressInput.dispatchEvent(new Event('input', { bubbles: true }));
                    addressInput.dispatchEvent(new Event('change', { bubbles: true }));
                }
            }
        } catch (err) {
            console.error("GetLocation error:", err);
        }
    }

    function hideDropdown() {
        if (dropdown) dropdown.classList.add('hidden');
    }

    document.addEventListener('click', function (e) {
        if (searchContainer && !searchContainer.contains(e.target)) {
            hideDropdown();
        }
    });
});
