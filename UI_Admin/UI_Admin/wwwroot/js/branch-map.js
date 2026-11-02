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

    function handleCoordsKeydown(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            handleManualCoordsInput();
        }
    }

    if (latInput) {
        latInput.readOnly = false;
        latInput.addEventListener('change', handleManualCoordsInput);
        latInput.addEventListener('paste', handleCoordsPaste);
        latInput.addEventListener('keydown', handleCoordsKeydown);
    }
    if (lngInput) {
        lngInput.readOnly = false;
        lngInput.addEventListener('change', handleManualCoordsInput);
        lngInput.addEventListener('paste', handleCoordsPaste);
        lngInput.addEventListener('keydown', handleCoordsKeydown);
    }

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

    let isProgrammaticUpdate = false;
    let lastGeocodedCoords = null;
    const reverseGeocodeCache = new Map();
    const inFlightReversePromises = new Map();

    function handleMarkerDragEnd() {
        if (!marker) return;
        const pos = marker.getLngLat();
        updateMarkerAndCoords(pos.lat, pos.lng);
        reverseGeocode(pos.lat, pos.lng);
    }

    function updateMarkerAndCoords(lat, lng) {
        const formattedLat = parseFloat(lat.toFixed(6));
        const formattedLng = parseFloat(lng.toFixed(6));

        isProgrammaticUpdate = true;
        try {
            if (latInput) {
                latInput.value = formattedLat;
                latInput.dispatchEvent(new Event('input', { bubbles: true }));
            }

            if (lngInput) {
                lngInput.value = formattedLng;
                lngInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
        } finally {
            setTimeout(() => { isProgrammaticUpdate = false; }, 60);
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
        const key = `${lat.toFixed(5)},${lng.toFixed(5)}`;
        if (reverseGeocodeCache.has(key)) {
            const cached = reverseGeocodeCache.get(key);
            applyAddress(cached);
            return cached;
        }

        if (inFlightReversePromises.has(key)) {
            return await inFlightReversePromises.get(key);
        }

        const fetchPromise = (async () => {
            try {
                const response = await fetch(`/Branches/ReverseLocation?latitude=${lat}&longitude=${lng}`);
                if (!response.ok) return null;

                const data = await response.json();
                const newAddress = data?.display || data?.address || data?.name;

                if (newAddress) {
                    reverseGeocodeCache.set(key, newAddress);
                    applyAddress(newAddress);
                }
                return newAddress;
            } catch (err) {
                console.warn("Reverse geocoding error:", err);
                return null;
            } finally {
                inFlightReversePromises.delete(key);
            }
        })();

        inFlightReversePromises.set(key, fetchPromise);
        return await fetchPromise;
    }

    function applyAddress(newAddress) {
        if (newAddress && addressInput) {
            addressInput.value = newAddress;
            addressInput.dispatchEvent(new Event('input', { bubbles: true }));
            addressInput.dispatchEvent(new Event('change', { bubbles: true }));

            if (searchInput) {
                searchInput.value = newAddress;
                if (clearSearchBtn) clearSearchBtn.classList.remove('hidden');
            }
        }
    }

    function handleManualCoordsInput() {
        if (isProgrammaticUpdate) return;
        const lat = latInput ? parseFloat(latInput.value) : NaN;
        const lng = lngInput ? parseFloat(lngInput.value) : NaN;

        if (!isNaN(lat) && !isNaN(lng) && lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180) {
            if (lastGeocodedCoords &&
                Math.abs(lastGeocodedCoords.lat - lat) < 0.00001 &&
                Math.abs(lastGeocodedCoords.lng - lng) < 0.00001) {
                return;
            }
            goToCoordinates(lat, lng);
        }
    }

    function handleCoordsPaste(e) {
        const pastedText = (e.clipboardData || window.clipboardData)?.getData('text');
        if (pastedText) {
            const coords = parseCoordinates(pastedText);
            if (coords) {
                e.preventDefault();
                goToCoordinates(coords.lat, coords.lng);
            }
        }
    }

    function parseCoordinates(query) {
        if (!query) return null;
        let text = query.trim();

        // 1. Google Maps URL or query string, e.g. @10.0376913,105.8307985 or ?q=10.0376913,105.8307985
        const urlMatch = text.match(/@(-?\d+\.\d+),(-?\d+\.\d+)/) || text.match(/[?&]q=(-?\d+\.\d+),(-?\d+\.\d+)/);
        if (urlMatch) {
            const pLat = parseFloat(urlMatch[1]);
            const pLng = parseFloat(urlMatch[2]);
            return validateAndNormalizeCoords(pLat, pLng);
        }

        // 2. Strip brackets, parentheses, and common labels
        text = text.replace(/^[\[\(]/, '').replace(/[\]\)]$/, '').trim();
        text = text.replace(/(?:latitude|lat|vĩ\s*độ)\s*[:=]?\s*/gi, '');
        text = text.replace(/(?:longitude|lng|long|kinh\s*độ)\s*[:=]?\s*/gi, '');

        // 3. Decimal Degree regex: handles "10.0376, 105.8307", "10.0376 105.8307", with optional ° / N / E
        const decimalRegex = /^([-+]?[0-9]*\.?[0-9]+)\s*[°]?[NSns]?\s*[\s,;/\t]+\s*([-+]?[0-9]*\.?[0-9]+)\s*[°]?[EWew]?$/;
        const match = text.match(decimalRegex);
        if (match) {
            const num1 = parseFloat(match[1]);
            const num2 = parseFloat(match[2]);
            return validateAndNormalizeCoords(num1, num2);
        }

        // 4. DMS regex (Degrees, Minutes, Seconds) e.g. 10°02'15.7"N 105°49'50.9"E
        const dmsRegex = /^(\d+)[°\s]+(\d+)['\s]+([\d.]+)["]?\s*([NSns])\s*[,;/\s]+\s*(\d+)[°\s]+(\d+)['\s]+([\d.]+)["]?\s*([EWew])$/i;
        const dmsMatch = text.match(dmsRegex);
        if (dmsMatch) {
            let lat = parseInt(dmsMatch[1], 10) + parseInt(dmsMatch[2], 10) / 60 + parseFloat(dmsMatch[3]) / 3600;
            if (dmsMatch[4].toUpperCase() === 'S') lat = -lat;
            let lng = parseInt(dmsMatch[5], 10) + parseInt(dmsMatch[6], 10) / 60 + parseFloat(dmsMatch[7]) / 3600;
            if (dmsMatch[8].toUpperCase() === 'W') lng = -lng;
            return validateAndNormalizeCoords(lat, lng);
        }

        return null;
    }

    function validateAndNormalizeCoords(num1, num2) {
        if (isNaN(num1) || isNaN(num2)) return null;

        let lat, lng;
        // In Vietnam, lat is ~8.5 to 23.4, lng is ~102.1 to 109.5
        // If first number is > 30 and second is <= 30, it was entered as [lng, lat]
        if (num1 > 30 && num2 <= 30) {
            lng = num1;
            lat = num2;
        } else {
            lat = num1;
            lng = num2;
        }

        if (lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180) {
            return { lat, lng };
        }
        return null;
    }

    async function goToCoordinates(lat, lng) {
        hideDropdown();
        // Always preserve exact coordinates requested by user
        updateMarkerAndCoords(lat, lng);
        if (map) {
            map.flyTo({
                center: [lng, lat],
                zoom: 16
            });
        }
        const resolvedAddress = await reverseGeocode(lat, lng);
        if (resolvedAddress) {
            applyAddress(resolvedAddress);
        }
    }

    function renderCoordinateOption(coords) {
        if (!dropdown) return;
        dropdown.innerHTML = '';

        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'suggestion-item flex items-start gap-3 text-left w-full p-3 hover:bg-surface-container transition-colors cursor-pointer border-b border-surface-container-high bg-primary/5';

        const iconContainer = document.createElement('div');
        iconContainer.className = 'w-8 h-8 rounded-lg bg-primary/20 text-primary flex items-center justify-center shrink-0 mt-0.5';
        iconContainer.innerHTML = '<span class="material-symbols-outlined text-[20px]">my_location</span>';

        const textContainer = document.createElement('div');
        textContainer.className = 'flex-1 min-w-0';

        const titleSpan = document.createElement('div');
        titleSpan.className = 'font-body-md-medium text-primary font-bold truncate';
        titleSpan.textContent = `📍 Go to coordinates: ${coords.lat.toFixed(6)}, ${coords.lng.toFixed(6)}`;

        const addrSpan = document.createElement('div');
        addrSpan.className = 'font-body-sm text-on-surface-variant truncate mt-0.5';
        addrSpan.textContent = 'Pin this exact location and look up nearby addresses without changing the coordinates';

        textContainer.appendChild(titleSpan);
        textContainer.appendChild(addrSpan);

        btn.appendChild(iconContainer);
        btn.appendChild(textContainer);

        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            goToCoordinates(coords.lat, coords.lng);
        });

        dropdown.appendChild(btn);
        dropdown.classList.remove('hidden');
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

            // Check if query is a coordinate pair
            const coords = parseCoordinates(query);
            if (coords) {
                renderCoordinateOption(coords);
                return;
            }

            debounceTimer = setTimeout(() => {
                fetchSuggestions(query);
            }, 400);
        });

        searchInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const query = this.value.trim();
                const coords = parseCoordinates(query);
                if (coords) {
                    goToCoordinates(coords.lat, coords.lng);
                } else if (dropdown && dropdown.children.length > 0 && !dropdown.classList.contains('hidden')) {
                    const firstBtn = dropdown.querySelector('.suggestion-item');
                    if (firstBtn) firstBtn.click();
                }
            }
        });

        searchInput.addEventListener('paste', function (e) {
            const pasted = (e.clipboardData || window.clipboardData)?.getData('text');
            if (pasted) {
                const coords = parseCoordinates(pasted);
                if (coords) {
                    e.preventDefault();
                    searchInput.value = pasted.trim();
                    if (clearSearchBtn) clearSearchBtn.classList.remove('hidden');
                    goToCoordinates(coords.lat, coords.lng);
                }
            }
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
