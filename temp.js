        const endpoints = {
            list: '@Url.Action("GetAll", "Travel")',
            get: '@Url.Action("Get", "Travel")',
            create: '@Url.Action("Create", "Travel")',
            update: '@Url.Action("Update", "Travel")',
            delete: '@Url.Action("Delete", "Travel")',
            employees: '@Url.Action("Employees", "Travel")',
            onsiteStatus: '@Url.Action("OnsiteStatus", "Travel")'
        };

        let travelTable;
        let employeeCache = {};
        let searchTimer;
        let pendingPayload = null;
        let pendingWarnings = [];
        const requiredDays = 42;
        let lastGapDays = null;

        $(document).ready(function () {
            initTable();
            bindForm();
            fetchEmployees();
        });

        async function fetchJson(url, options) {
            const response = await fetch(url, options);
            const text = await response.text();
            try {
                return JSON.parse(text);
            } catch (err) {
                throw new Error(text || 'Invalid response');
            }
        }

        function initTable() {
            travelTable = $('#travelTable').DataTable({
                ajax: {
                    url: endpoints.list,
                    type: 'GET',
                    dataSrc: 'data'
                },
                order: [[1, 'desc']],
                columns: [
                    {
                        data: null,
                        render: function (data) {
                            return `
                                <div class="font-weight-bold text-dark">${data.nama || '-'}</div>
                                <div class="text-muted-small">NIK: ${data.nik || '-'} | DEPT: ${data.kd_depart || '-'}</div>
                                <div class="text-muted-small">${data.departemen || '-'}</div>`;
                        }
                    },
                    {
                        data: null,
                        render: function (data) {
                            const out = formatDate(data.out_site);
                            const on = formatDate(data.on_site);
                            const diff = dayDiff(data.out_site, data.on_site);
                            return `
                                <div class="period-chip d-flex align-items-center">
                                    <span class="badge badge-out mr-2">${out}</span>
                                    <i class="fas fa-arrow-right text-muted mr-2"></i>
                                    <span class="badge badge-on">${on}</span>
                                </div>
                                <div class="text-muted-small">Durasi ${diff} hari</div>`;
                        }
                    },
                    {
                        data: null,
                        render: function (data) {
                            return `
                                <div class="font-weight-semibold">${data.wilayah || '-'}</div>
                                <div class="text-muted-small">${data.poh || '-'}</div>
                            `;
                        }
                    },
                    {
                        data: 'nominal',
                        render: function (data) {
                            return `<span class="font-weight-bold text-primary">${formatNominal(data)}</span>`;
                        }
                    },
                    {
                        data: 'nomor_ta',
                        render: (data, type, row) => `
                            <div class="font-weight-semibold">${data || '-'}</div>
                            <div class="text-muted-small">By ${row.created_by || '-'}</div>
                        `
                    },
                    {
                        data: 'id',
                        orderable: false,
                        className: 'text-center',
                        render: data => `
                            <button class="btn btn-outline-primary action-btn btn-edit" data-id="${data}" title="Ubah">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-outline-danger action-btn btn-delete ml-1" data-id="${data}" title="Hapus">
                                <i class="fas fa-trash"></i>
                            </button>
                        `
                    }
                ],
                language: {
                    emptyTable: "Data belum ada",
                    zeroRecords: "Data belum ada"
                }
            });

            $('#travelTable').on('click', '.btn-edit', function () {
                const id = $(this).data('id');
                editTravel(id);
            });

            $('#travelTable').on('click', '.btn-delete', function () {
                const id = $(this).data('id');
                deleteTravel(id);
            });

            travelTable.on('xhr', function () {
                const json = travelTable.ajax.json() || {};
                const rows = json.data || [];
                updateCounters(rows);
            });
        }

        function bindForm() {
            $('#travelForm').on('submit', function (e) {
                e.preventDefault();
                submitForm();
            });

            $('#resetBtn').on('click', function () {
                resetForm();
            });

            $('#nik').on('input', function () {
                const term = $(this).val();
                if (searchTimer) clearTimeout(searchTimer);
                searchTimer = setTimeout(function () {
                    if (term.length >= 2) {
                        fetchEmployees(term);
                    }
                    renderSuggestions(term);
                }, 300);
            });

            $('#nik').on('change', function () {
                fillEmployeeInfo($(this).val());
                updateOnsiteStatus();
            });

            $('#out_site').on('change', function () {
                autoFillPeriodAndNumber();
                updateOnsiteStatus();
            });

            $('#on_site').on('change', function () {
                autoFillPeriodAndNumber();
            });

            $('.nominal-chip').on('click', function () {
                const val = $(this).data('val');
                setNominalValue(val);
            });

            $('#nominal').on('input blur', function () {
                const raw = $(this).val();
                const numeric = Number(String(raw || '').replace(/[^\d]/g, ''));
                if (!isNaN(numeric) && numeric > 0) {
                    $(this).val(numeric.toLocaleString('id-ID'));
                } else {
                    $(this).val('');
                }
            });

            $('#confirmProceedBtn').on('click', function () {
                $('#confirmModal').modal('hide');
                if (pendingPayload) {
                    submitPayload(pendingPayload);
                }
            });

            $(document).on('click', function (e) {
                if (!$(e.target).closest('#employeeSuggest, #nik').length) {
                    $('#employeeSuggest').hide();
                }
            });
        }

        function fetchEmployees(keyword = '') {
            fetchJson(`${endpoints.employees}?keyword=${encodeURIComponent(keyword)}`)
                .then(result => {
                    const data = result.data || [];
                    employeeCache = {};

                    data.forEach(item => {
                        employeeCache[item.no_nik] = item;
                    });

                    const nik = $('#nik').val();
                    if (nik) {
                        fillEmployeeInfo(nik);
                    }
                })
                .catch(() => {
                    alertify.error('Gagal memuat data karyawan.');
                });
        }

        function fillEmployeeInfo(nik) {
            const data = employeeCache[nik];
            $('#infoNama').text(data ? data.nama_lengkap || '-' : '-');
            $('#infoDept').text(data ? data.depart || '-' : '-');
            $('#infoPosisi').text(data ? data.posisi || '-' : '-');
            if (data) {
                if ($('#wilayah').val().trim() === '' && data.lokterima) {
                    $('#wilayah').val(data.lokterima);
                }
                if ($('#poh').val().trim() === '' && data.poh) {
                    $('#poh').val(data.poh);
                }
                if (($('#nominal').val() || '').trim() === '' && data.nominal) {
                    setNominalValue(data.nominal);
                }
            }
            autoFillPeriodAndNumber();
        }

        function renderSuggestions(term) {
            const panel = $('#employeeSuggest');
            if (!term || term.length < 2) {
                panel.hide();
                return;
            }

            const items = Object.values(employeeCache)
                .filter(x =>
                    (x.no_nik && x.no_nik.toLowerCase().includes(term.toLowerCase())) ||
                    (x.nama_lengkap && x.nama_lengkap.toLowerCase().includes(term.toLowerCase()))
                )
                .slice(0, 10);

            if (!items.length) {
                panel.hide();
                return;
            }

            const html = items.map(x => `
                <div class="suggestion-item" data-nik="${x.no_nik}">
                    <div class="suggestion-title">${x.nama_lengkap || '-'} <span class="text-muted">(${x.no_nik || '-'})</span></div>
                    <div class="suggestion-meta">${x.depart || '-'} - ${x.posisi || '-'} - POH: ${x.poh || '-'}</div>
                    <div class="suggestion-meta">Rekom nominal: ${formatNominal(x.nominal) || '-'}</div>
                </div>
            `).join('');

            panel.html(html).show();

            panel.find('.suggestion-item').on('click', function () {
                const nik = $(this).data('nik');
                $('#nik').val(nik);
                fillEmployeeInfo(nik);
                updateOnsiteStatus();
                panel.hide();
            });
        }

        function submitForm() {
            const payload = {
                id: $('#travelId').val() || null,
                nik: ($('#nik').val() || '').trim(),
                out_site: $('#out_site').val(),
                on_site: $('#on_site').val(),
                wilayah: $('#wilayah').val(),
                poh: $('#poh').val(),
                nominal: $('#nominal').val(),
                nomor_ta: $('#nomor_ta').val()
            };

            if (!payload.nik || !payload.out_site || !payload.on_site) {
                alertify.error('NIK, Out Site, dan On Site wajib diisi.');
                return;
            }

            const validation = validateOutDate(payload.nik, payload.out_site, payload.on_site);
            if (!validation.ok || validation.reasons.length) {
                pendingPayload = payload;
                pendingWarnings = validation.reasons;
                renderConfirmModal(validation.reasons);
                return;
            }

            submitPayload(payload);
        }

        function submitPayload(payload) {
            toggleSubmit(true);
            const url = payload.id ? endpoints.update : endpoints.create;

            fetchJson(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            })
                .then(result => {
                    if (result.success) {
                        alertify.success(result.message || 'Data tersimpan.');
                        if (result.warning) {
                            alertify.warning(result.warning);
                        }
                        resetForm();
                        travelTable.ajax.reload(null, false);
                    } else {
                        alertify.error(result.message || 'Gagal menyimpan data.');
                    }
                })
                .catch(() => alertify.error('Terjadi kesalahan saat menyimpan data.'))
                .finally(() => {
                    pendingPayload = null;
                    pendingWarnings = [];
                    toggleSubmit(false);
                });
        }

        function editTravel(id) {
            fetchJson(`${endpoints.get}?id=${id}`)
                .then(result => {
                    if (!result.success) {
                        alertify.error(result.message || 'Data tidak ditemukan.');
                        return;
                    }

                    const data = result.data;
                    $('#travelId').val(data.id);
                    $('#nik').val(data.nik);
                    $('#out_site').val(toInputDate(data.out_site));
                    $('#on_site').val(toInputDate(data.on_site));
                    $('#wilayah').val(data.wilayah);
                    $('#poh').val(data.poh);
                    $('#nominal').val(data.nominal);
                    $('#nomor_ta').val(data.nomor_ta);
                    fillEmployeeInfo(data.nik);
                    autoFillPeriodAndNumber();
                    $('#submitBtn .btn-label').html('<i class="fas fa-save mr-1"></i> Update Travel');
                    $('#formMode').text('Mode: Edit');
                    $('html, body').animate({ scrollTop: 0 }, 300);
                })
                .catch(() => alertify.error('Terjadi kesalahan saat mengambil data.'));
        }

        function deleteTravel(id) {
            alertify.confirm('Hapus data', 'Anda yakin ingin menghapus travel ini?', function () {
                fetchJson(`${endpoints.delete}?id=${id}`, { method: 'POST' })
                    .then(result => {
                        if (result.success) {
                            alertify.success(result.message || 'Data dihapus.');
                            travelTable.ajax.reload(null, false);
                        } else {
                            alertify.error(result.message || 'Gagal menghapus data.');
                        }
                    })
                    .catch(() => alertify.error('Terjadi kesalahan saat menghapus data.'));
            }, function () { });
        }

        function resetForm() {
            $('#travelId').val('');
            $('#travelForm')[0].reset();
            $('#submitBtn .btn-label').html('<i class="fas fa-save mr-1"></i> Simpan Travel');
            $('#formMode').text('Mode: Tambah');
            fillEmployeeInfo('');
            $('#periodBadge').text('Periode: -');
            $('#nomor_ta').val('');
            $('.nominal-chip').removeClass('active');
            $('#onsiteDate').text('-');
            $('#onsiteGap').text('- hari');
            $('#onsiteMessage').text('');
            setOnsiteBadge('badge-soft-warning', 'Tidak diketahui');
        }

        function toggleSubmit(isLoading) {
            const btn = $('#submitBtn');
            btn.prop('disabled', isLoading);
            if (isLoading) {
                btn.find('.btn-label').html('<span class="spinner-border spinner-border-sm mr-2"></span>Memproses...');
            } else {
                const mode = $('#travelId').val() ? 'Update Travel' : 'Simpan Travel';
                btn.find('.btn-label').html(`<i class="fas fa-save mr-1"></i> ${mode}`);
            }
        }

        function formatDate(value) {
            if (!value) return '-';
            const date = new Date(value);
            if (isNaN(date)) return '-';
            return date.toLocaleDateString('id-ID', { year: 'numeric', month: 'short', day: 'numeric' });
        }

        function toInputDate(value) {
            if (!value) return '';
            const date = new Date(value);
            if (isNaN(date)) return '';
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            return `${date.getFullYear()}-${month}-${day}`;
        }

        function formatNominal(value) {
            const numeric = Number(String(value || '').replace(/[.,]/g, ''));
            if (!isNaN(numeric) && numeric > 0) {
                return numeric.toLocaleString('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 });
            }
            return value || '-';
        }

        function setNominalValue(value) {
            const numeric = Number(String(value || '').replace(/[.,]/g, ''));
            const chips = $('.nominal-chip');
            chips.removeClass('active');

            if (!isNaN(numeric) && numeric > 0) {
                const matched = chips.filter((i, el) => Number($(el).data('val')) === numeric);
                if (matched.length) {
                    matched.addClass('active');
                }
                $('#nominal').val(numeric.toLocaleString('id-ID'));
            } else {
                $('#nominal').val('');
                $('#nominal').focus();
            }
        }

        function dayDiff(start, end) {
            const startDate = new Date(start);
            const endDate = new Date(end);
            if (isNaN(startDate) || isNaN(endDate)) return '-';
            const diff = Math.round((endDate - startDate) / (1000 * 60 * 60 * 24)) + 1;
            return diff > 0 ? diff : '-';
        }

        function updateCounters(rows) {
            $('#totalTravel').text(rows.length);
            const now = new Date();
            let ongoing = 0;
            let next = null;

            rows.forEach(item => {
                const start = new Date(item.out_site);
                const end = new Date(item.on_site);
                if (!isNaN(start) && !isNaN(end)) {
                    if (start <= now && end >= now) {
                        ongoing = 1;
                    }
                    if (start > now && (!next || start < next)) {
                        next = start;
                    }
                }
            });

            $('#ongoingTravel').text(ongoing);
            $('#nextTravel').text(next ? formatDate(next) : '-');
        }

        function autoFillPeriodAndNumber() {
            const outVal = $('#out_site').val();
            const onVal = $('#on_site').val();

            if (!outVal) {
                $('#periodBadge').text('Periode: -');
                $('#nomor_ta').val('');
                return;
            }

            const outDate = new Date(outVal);
            if (isNaN(outDate)) return;

            $('#nomor_ta').val(generateNomorTA(outDate));

            if (onVal) {
                const onDate = new Date(onVal);
                if (!isNaN(onDate)) {
                    $('#periodBadge').text(`Periode: ${formatDate(outDate)} - ${formatDate(onDate)}`);
                } else {
                    $('#periodBadge').text('Periode: -');
                }
            } else {
                $('#periodBadge').text('Periode: -');
            }
        }

        function generateNomorTA(date) {
            const isoWeek = getIsoWeek(date);
            const base = isoWeek * 3;
            const day = date.getDay();
            const running = day === 1 ? base - 2 : day === 4 ? base - 1 : day === 6 ? base : base - 2;
            const roman = monthToRoman(date.getMonth() + 1);
            return `${running}/HC/TA/${roman}/${date.getFullYear()}`;
        }
        function updateOnsiteStatus() {
            const nik = ($('#nik').val() || '').trim();
            const outVal = $('#out_site').val();
            if (!nik || !outVal) return;

            fetchJson(`${endpoints.onsiteStatus}?nik=${encodeURIComponent(nik)}&outDate=${encodeURIComponent(outVal)}`)
                .then(res => {
                    if (!res.success) {
                        $('#onsiteDate').text('-');
                        $('#onsiteGap').text('- hari');
                        $('#onsiteMessage').text(res.message || '');
                        setOnsiteBadge('badge-soft-warning', 'Tidak diketahui');
                        return;
                    }

                    const last = res.last_on_site ? formatDate(res.last_on_site) : '-';
                    const gap = res.gap_days != null ? res.gap_days : null;
                    $('#onsiteDate').text(last);
                    $('#onsiteGap').text(gap != null ? `${gap} hari` : '- hari');
                    lastGapDays = gap;

                    if (gap == null) {
                        $('#onsiteMessage').text('Belum ada riwayat TA.');
                        setOnsiteBadge('badge-soft-warning', 'Baru');
                    } else if (gap >= requiredDays) {
                        $('#onsiteMessage').text(`Memenuhi target ${requiredDays} hari.`);
                        setOnsiteBadge('badge-soft-success', 'Sesuai');
                    } else {
                        $('#onsiteMessage').text(`Baru ${gap} hari, target ${requiredDays} hari.`);
                        setOnsiteBadge('badge-soft-danger', 'Belum cukup');
                    }
                })
                .catch((err) => {
                    $('#onsiteMessage').text('Gagal cek status onsite.');
                    setOnsiteBadge('badge-soft-warning', 'Error');
                    console.error('OnsiteStatus error:', err);
                });
        }

        function setOnsiteBadge(cls, text) {
            const el = $('#onsiteBadge');
            el.removeClass('badge-soft-warning badge-soft-success badge-soft-danger').addClass(cls).text(text);
        }

        function validateOutDate(nik, outSite, onSite) {
            const reasons = [];
            const date = new Date(outSite);
            if (isNaN(date)) return { ok: false, reasons: ['Tanggal Out Site tidak valid'] };

            const day = date.getDay(); // 0 Sunday
            const isSpecial = isSpecialNik(nik);
            const isAllowedDay = (day === 1 || day === 4 || day === 6); // Mon/Thu/Sat
            const isEvenDate = date.getDate() % 2 === 0;

            if (!isAllowedDay && !isSpecial) {
                reasons.push('Out Site di luar jadwal Sen/Kam/Sab.');
            }

            if (isSpecial && !isEvenDate) {
                reasons.push('Out Site untuk NIK ini sebaiknya di tanggal genap.');
            }

            const today = new Date();
            today.setHours(0, 0, 0, 0);
            const leadDaysOut = Math.round((date - today) / 86400000);
            if (leadDaysOut < 14) {
                reasons.push('Pengajuan Out Site kurang dari 14 hari.');
            }

            if (onSite) {
                const onDate = new Date(onSite);
                if (!isNaN(onDate)) {
                    const leadDaysOn = Math.round((onDate - today) / 86400000);
                    if (leadDaysOn < 14) {
                        reasons.push('On Site kurang dari 14 hari dari hari ini.');
                    }
                }
            }

            if (lastGapDays != null && lastGapDays < requiredDays) {
                reasons.push(`Jeda onsite baru ${lastGapDays} hari, target ${requiredDays} hari.`);
            }

            return { ok: true, reasons };
        }

        function isSpecialNik(nik) {
            return (nik || '').trim() === '23051670785';
        }

        function renderConfirmModal(reasons) {
            const list = $('#confirmReasons');
            list.empty();
            reasons.forEach(r => list.append(`<li>${r}</li>`));
            $('#confirmModal').modal('show');
        }

        function getIsoWeek(date) {
            const target = new Date(date.valueOf());
            const dayNr = (target.getDay() + 6) % 7;
            target.setDate(target.getDate() - dayNr + 3);
            const firstThursday = new Date(target.getFullYear(), 0, 4);
            const firstThursdayDayNr = (firstThursday.getDay() + 6) % 7;
            firstThursday.setDate(firstThursday.getDate() - firstThursdayDayNr + 3);
            const weekNumber = 1 + Math.round(((target - firstThursday) / 86400000 - 3) / 7);
            return weekNumber;
        }

        function monthToRoman(month) {
            const map = ["I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII"];
            return map[month - 1] || "";
        }

