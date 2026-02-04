(function ($) {
	'use strict';

	const totalSteps = 6;
	let currentStep = 1;
	const form = $('#formInputKaryawan');
	const btnNext = $('#btnNext');
	const btnBack = $('#btnBack');
	const confirmationContainer = $('#confirmation-container');
	const pendidikanOptions = parsePendidikanOptions();
	const DOC_REQUIREMENTS = parseDocRequirements();
	const INVITE_CONTEXT = parseInviteContext();
	const identityField = $('#no_identitas');
	const nationalityField = $('#kewarganegaraan');

	function parsePendidikanOptions() {
		try {
			if (listPendidikanJsonData && listPendidikanJsonData.length) {
				return JSON.parse(listPendidikanJsonData);
			}
		} catch (err) {
			console.error('Failed to parse pendidikan options', err);
		}
		return [];
	}

	function init() {
		initWizard();
		initSelect2();
		initDynamicLists();
		initFileInputs();
		wireupFormSubmit();
		autoGenerateIndexim();
		initGeneralBehaviors();
		initInviteContext();
		applyIdentityRules();
	}

	function initWizard() {
		showStep(currentStep);
		btnBack.on('click', function () {
			if (currentStep === 1) return;
			currentStep -= 1;
			showStep(currentStep);
		});
		btnNext.on('click', async function () {
			if (!validateStep(currentStep)) return;
			if (currentStep < totalSteps) {
				currentStep += 1;
				showStep(currentStep);
				if (currentStep === totalSteps) {
					renderConfirmation(buildPayload(false));
				}
			} else {
				await submitWizard();
			}
		});
	}

	function showStep(step) {
		$('.wizard-step-panel').removeClass('active');
		$('#step-' + step).addClass('active');

		$('.stepper-item').each(function () {
			const stepNumber = parseInt($(this).data('step'), 10);
			$(this).toggleClass('active', stepNumber === step);
			$(this).toggleClass('completed', stepNumber < step);
		});

		btnBack.prop('disabled', step === 1);
		btnNext.text(step === totalSteps ? 'Simpan Semua Data' : 'Berikutnya');
	}

	function validateStep(step) {
		const panel = $('#step-' + step);
		const inputs = panel.find('input, select, textarea').filter('[required]');
		let isValid = true;
		inputs.each(function () {
			if (!this.checkValidity()) {
				this.reportValidity();
				isValid = false;
				return false;
			}
		});
		return isValid;
	}

	function initSelect2() {
		$('.select2-dynamic').each(function () {
			const $el = $(this);
			const placeholder = $el.data('placeholder') || '-- Pilih --';
			$el.select2({
				theme: 'bootstrap4',
				width: '100%',
				placeholder,
				allowClear: true,
				ajax: {
					delay: 250,
					url: '/InputKaryawan/Lookup',
					data: function (params) {
						return {
							entity: $el.data('entity'),
							q: params.term || '',
							parentId: getParentValue($el)
						};
					},
					processResults: function (resp) {
						const data = (resp && resp.data) ? resp.data : [];
						return { results: data };
					}
				}
			});

			const parentId = $el.data('parent-id');
			if (parentId) {
				const $parent = $('#' + parentId);
				$parent.on('change', function () {
					const hasValue = !!$(this).val();
					if (!hasValue) {
						$el.val(null).trigger('change');
					}
					$el.prop('disabled', !hasValue);
				});
				$el.prop('disabled', !$parent.val());
			}
		});
	}

	function getParentValue($el) {
		const parentId = $el.data('parent-id');
		if (!parentId) return null;
		return $('#' + parentId).val();
	}

	function initDynamicLists() {
		addKeluargaItem();
		addPendidikanItem();
		addSertifikasiItem();
		addMcuItem();
		addVaksinItem();

		$('#btnAddKeluarga').on('click', addKeluargaItem);
		$('#btnAddPendidikan').on('click', addPendidikanItem);
		$('#btnAddSertifikasi').on('click', addSertifikasiItem);
		$('#btnAddMcu').on('click', addMcuItem);
		$('#btnAddVaksin').on('click', addVaksinItem);

		$(document).on('click', '.btn-remove-item', function () {
			const target = $(this).data('target');
			const $row = $(this).closest('.dynamic-list-item');
			$row.remove();
			if ($('.' + target + '-item').length === 0) {
				switch (target) {
					case 'keluarga': addKeluargaItem(); break;
					case 'pendidikan': addPendidikanItem(); break;
					case 'sertifikasi': addSertifikasiItem(); break;
					case 'mcu': addMcuItem(); break;
					case 'vaksin': addVaksinItem(); break;
				}
			}
		});
	}

	function addKeluargaItem() {
		const index = $('.keluarga-item').length;
		const template = `
			<div class="dynamic-list-item keluarga-item" data-index="${index}">
				<button type="button" class="btn btn-sm btn-danger btn-remove-item" data-target="keluarga">&times;</button>
				<div class="row">
					<div class="col-md-3 form-group">
						<label>Hubungan</label>
						<input type="text" class="form-control input-keluarga-hubungan force-uppercase" placeholder="SUAMI / ISTRI / ANAK">
					</div>
					<div class="col-md-3 form-group">
						<label>Nama Lengkap</label>
						<input type="text" class="form-control input-keluarga-nama force-uppercase" placeholder="Nama">
					</div>
					<div class="col-md-3 form-group">
						<label>Tanggal Lahir</label>
						<input type="date" class="form-control input-keluarga-tanggal">
					</div>
					<div class="col-md-3 form-group">
						<label>Pekerjaan</label>
						<input type="text" class="form-control input-keluarga-pekerjaan force-uppercase" placeholder="Pekerjaan">
					</div>
					<div class="col-md-3 form-group">
						<label>No. HP</label>
						<input type="text" class="form-control input-keluarga-hp force-numeric" placeholder="08xxxxxxxx">
					</div>
					<div class="col-md-3 form-group">
						<label class="d-block">Tanggungan</label>
						<div class="form-check form-check-inline">
							<input class="form-check-input input-keluarga-tanggungan" type="checkbox">
							<label class="form-check-label">Ya</label>
						</div>
					</div>
				</div>
			</div>`;
		$('#keluarga-list-container').append(template);
	}

	function addPendidikanItem() {
		const index = $('.pendidikan-item').length;
		const options = ['<option value="">-- Pilih Pendidikan --</option>']
			.concat(pendidikanOptions.map(opt => `<option value="${opt.id}">${opt.text}</option>`))
			.join('');
		const template = `
			<div class="dynamic-list-item pendidikan-item" data-index="${index}">
				<button type="button" class="btn btn-sm btn-danger btn-remove-item" data-target="pendidikan">&times;</button>
				<div class="row">
					<div class="col-md-3 form-group">
						<label>Pendidikan</label>
						<select class="form-control input-pendidikan-id">${options}</select>
					</div>
					<div class="col-md-3 form-group">
						<label>Nama Institusi</label>
						<input type="text" class="form-control input-pendidikan-nama force-uppercase" placeholder="Nama Sekolah / Kampus">
					</div>
					<div class="col-md-2 form-group">
						<label>Tahun Masuk</label>
						<input type="text" class="form-control input-pendidikan-masuk force-numeric" maxlength="4">
					</div>
					<div class="col-md-2 form-group">
						<label>Tahun Lulus</label>
						<input type="text" class="form-control input-pendidikan-lulus force-numeric" maxlength="4">
					</div>
					<div class="col-md-2 form-group">
						<label>File Ijazah</label>
						<input type="file" class="form-control-file input-pendidikan-file" name="pendidikan_file_${index}" accept="application/pdf,image/*">
					</div>
				</div>
			</div>`;
		$('#pendidikan-list-container').append(template);
	}

	function addSertifikasiItem() {
		const index = $('.sertifikasi-item').length;
		const template = `
			<div class="dynamic-list-item sertifikasi-item" data-index="${index}">
				<button type="button" class="btn btn-sm btn-danger btn-remove-item" data-target="sertifikasi">&times;</button>
				<div class="row">
					<div class="col-md-3 form-group">
						<label>Nama Sertifikasi</label>
						<input type="text" class="form-control input-sertifikasi-nama force-uppercase" placeholder="Nama Sertifikasi">
					</div>
					<div class="col-md-3 form-group">
						<label>Lembaga</label>
						<input type="text" class="form-control input-sertifikasi-lembaga force-uppercase" placeholder="Penerbit">
					</div>
					<div class="col-md-2 form-group">
						<label>No. Sertifikat</label>
						<input type="text" class="form-control input-sertifikasi-nomor force-uppercase">
					</div>
					<div class="col-md-2 form-group">
						<label>Tanggal Terbit</label>
						<input type="date" class="form-control input-sertifikasi-terbit">
					</div>
					<div class="col-md-2 form-group">
						<label>Kadaluarsa</label>
						<input type="date" class="form-control input-sertifikasi-expired">
					</div>
					<div class="col-md-4 form-group">
						<label>File Sertifikat</label>
						<input type="file" class="form-control-file input-sertifikasi-file" name="sertifikasi_file_${index}" accept="application/pdf,image/*">
					</div>
				</div>
			</div>`;
		$('#sertifikasi-list-container').append(template);
	}

	function addMcuItem() {
		const index = $('.mcu-item').length;
		const template = `
			<div class="dynamic-list-item mcu-item" data-index="${index}">
				<button type="button" class="btn btn-sm btn-danger btn-remove-item" data-target="mcu">&times;</button>
				<div class="row">
					<div class="col-md-4 form-group">
						<label>Hasil MCU</label>
						<input type="text" class="form-control input-mcu-hasil force-uppercase" placeholder="Sehat / Dengan Catatan">
					</div>
					<div class="col-md-3 form-group">
						<label>Faskes</label>
						<input type="text" class="form-control input-mcu-faskes force-uppercase" placeholder="Nama RS/Klinik">
					</div>
					<div class="col-md-3 form-group">
						<label>Tanggal MCU</label>
						<input type="date" class="form-control input-mcu-tanggal">
					</div>
					<div class="col-md-2 form-group">
						<label>File Hasil</label>
						<input type="file" class="form-control-file input-mcu-file" name="mcu_file_${index}" accept="application/pdf,image/*">
					</div>
				</div>
			</div>`;
		$('#mcu-list-container').append(template);
	}

	function addVaksinItem() {
		const index = $('.vaksin-item').length;
		const template = `
			<div class="dynamic-list-item vaksin-item" data-index="${index}">
				<button type="button" class="btn btn-sm btn-danger btn-remove-item" data-target="vaksin">&times;</button>
				<div class="row">
					<div class="col-md-3 form-group">
						<label>Jenis Vaksin</label>
						<input type="text" class="form-control input-vaksin-jenis force-uppercase">
					</div>
					<div class="col-md-2 form-group">
						<label>Dosis Ke</label>
						<input type="number" class="form-control input-vaksin-dosis" min="1" value="1">
					</div>
					<div class="col-md-3 form-group">
						<label>Tanggal Vaksin</label>
						<input type="date" class="form-control input-vaksin-tanggal">
					</div>
					<div class="col-md-4 form-group">
						<label>Faskes / Catatan</label>
						<input type="text" class="form-control input-vaksin-catatan force-uppercase">
					</div>
				</div>
			</div>`;
		$('#vaksin-list-container').append(template);
	}

	function initFileInputs() {
		$(document).on('change', '.custom-file-input', function () {
			const fileName = $(this).val().split('\\').pop();
			$(this).siblings('.custom-file-label').text(fileName || 'Pilih file...');
		});

		$('#file_pasfoto').on('change', function () {
			const file = this.files && this.files[0];
			if (!file) return;
			const reader = new FileReader();
			reader.onload = function (e) {
				$('#file_pasfoto_preview').attr('src', e.target.result);
			};
			reader.readAsDataURL(file);
		});
	}

	function parseDocRequirements() {
		try {
			const el = document.getElementById('doc-config');
			if (el && el.textContent) {
				return JSON.parse(el.textContent);
			}
		} catch (err) {
			console.warn('Failed to parse document requirement config', err);
		}
		return [];
	}

	function parseInviteContext() {
		try {
			const el = document.getElementById('invite-config');
			if (el && el.textContent) {
				return JSON.parse(el.textContent) || {};
			}
		} catch (err) {
			console.warn('Failed to parse invite context', err);
		}
		return {};
	}

	function wireupFormSubmit() {
		form.on('submit', function (e) {
			e.preventDefault();
		});
	}

	function initGeneralBehaviors() {
		nationalityField.on('change', function () {
			applyIdentityRules();
			handleIdentityBlur();
		});

		const lokasiKerja = $('#lokasi_kerja_id');
		const lokasiText = $('#lokasi_kerja_text');
		lokasiKerja.on('change', function () {
			const show = $(this).val() === 'other';
			lokasiText.toggle(show);
		});

		identityField.on('blur', handleIdentityBlur);
	}

	function initInviteContext() {
		if (!INVITE_CONTEXT || !INVITE_CONTEXT.Token) return;
		if (INVITE_CONTEXT.Nik) {
			identityField.val(INVITE_CONTEXT.Nik).prop('readonly', true);
			autoGenerateIndexim(INVITE_CONTEXT.Nik);
		}
		if (INVITE_CONTEXT.Nama) {
			$('#nama_lengkap').val(INVITE_CONTEXT.Nama).prop('readonly', true);
		}
		if (INVITE_CONTEXT.CompanyId) {
			const perusahaanSelect = $('#perusahaan_id');
			if (perusahaanSelect.length) {
				const optionText = perusahaanSelect.find(`option[value='${INVITE_CONTEXT.CompanyId}']`).text() || 'Perusahaan Undangan';
				if (!perusahaanSelect.find(`option[value='${INVITE_CONTEXT.CompanyId}']`).length) {
					perusahaanSelect.append(new Option(optionText, INVITE_CONTEXT.CompanyId, true, true));
				}
				perusahaanSelect.val(INVITE_CONTEXT.CompanyId).trigger('change').prop('disabled', true);
			}
		}
	}

	function applyIdentityRules() {
		const isWna = nationalityField.val() === 'WNA';
		const label = $('#label_no_identitas');
		const normalized = getNormalizedIdentity();
		if (isWna) {
			label.text('No. Paspor');
			identityField.removeClass('force-numeric').addClass('force-uppercase-alphanum');
			identityField.attr('placeholder', 'Nomor paspor (boleh huruf/angka)').attr('maxlength', '25');
		} else {
			label.text('No. KTP');
			identityField.removeClass('force-uppercase-alphanum').addClass('force-numeric');
			identityField.attr('placeholder', '16 digit nomor KTP').attr('maxlength', '16');
		}
		if (normalized) {
			identityField.val(normalized);
		}
	}

	function getNormalizedIdentity() {
		const raw = identityField.val() || '';
		const cleaned = raw.replace(/[^0-9a-z]/gi, '').toUpperCase();
		if (!cleaned) return '';
		const isWna = nationalityField.val() === 'WNA';
		if (isWna) return cleaned;
		const digitsOnly = cleaned.replace(/\D/g, '');
		return digitsOnly || cleaned;
	}

	function handleIdentityBlur() {
		const normalized = getNormalizedIdentity();
		if (normalized && normalized.length >= 6) {
			autoGenerateIndexim(normalized);
		}
	}

	async function autoGenerateIndexim(nikValue) {
		try {
			const identifier = nikValue || getNormalizedIdentity();
			const query = identifier ? `?nik=${encodeURIComponent(identifier)}` : '';
			const response = await fetch(`/InputKaryawan/GenerateIndeximId${query}`);
			const result = await response.json();
			if (result.success && result.data) {
				$('#indexim_id').val(result.data);
			}
		} catch (error) {
			console.warn('Gagal mengambil Indexim ID', error);
		}
	}

	function buildPayload(includeDocuments = true) {
		const profile = {
			indexim_id: $('#indexim_id').val(),
			kewarganegaraan: $('#kewarganegaraan').val(),
			no_identitas: getNormalizedIdentity() || $('#no_identitas').val(),
			no_kk: $('#no_kk').val(),
			nama_lengkap: $('#nama_lengkap').val(),
			tempat_lahir: $('#tempat_lahir').val(),
			tanggal_lahir: $('#tanggal_lahir').val(),
			jenis_kelamin: $('#jenis_kelamin').val(),
			gol_darah: $('#gol_darah').val(),
			agama: $('#agama').val(),
			status_residence_id: $('#status_residence_id').val(),
			status_reciden: $('input[name="status_reciden"]:checked').val() === 'true',
			no_telp_pribadi: $('#no_telp_pribadi').val(),
			email_pribadi: $('#email_pribadi').val(),
			email_perusahaan: $('#email_perusahaan').val(),
			no_bpjskes: $('#no_bpjskes').val(),
			no_bpjstk: $('#no_bpjstk').val(),
			no_npwp: $('#no_npwp').val(),
			nama_ibu_kandung: $('#nama_ibu_kandung').val(),
			status_ibu: $('#status_ibu').val(),
			nama_ayah_kandung: $('#nama_ayah_kandung').val(),
			status_ayah: $('#status_ayah').val()
		};

		const parseIntValue = (value) => {
			const parsed = parseInt(value, 10);
			return Number.isNaN(parsed) ? null : parsed;
		};

		const perusahaanId = $('#perusahaan_id').val();
		const perusahaanText = $('#perusahaan_id option:selected').text();

		const job = {
			perusahaan_id: perusahaanId || null,
			perusahaan_ref_id: parseIntValue(perusahaanId),
			perusahaan_text: perusahaanText || null,
			departemen_id: parseIntValue($('#departemen_id').val()),
			section_id: null,
			posisi_id: null,
			posisi_text: $('#posisi_text').val(),
			job_level: $('#level_id').val(),
			job_grade: $('#grade_id').val(),
			roster_code: $('#roster_id').val(),
			lokasi_kerja_kode: $('#lokasi_kerja_id').val() || null,
			lokasi_kerja_text: $('#lokasi_kerja_text').val(),
			lokasi_terima_text: $('#lokasi_terima_text').val(),
			nik: $('#nik').val(),
			doh: $('#doh').val(),
			poh: $('#poh').val(),
			tanggal_aktif: $('#tanggal_aktif').val(),
			tanggal_resign: $('#tanggal_resign').val(),
			status_residence: $('#status_residence_id').val(),
			status_reciden: $('input[name="status_reciden"]:checked').val() === 'true'
		};

		const addresses = [];
		const provinceText = $('#provinsi_id option:selected').text() || $('#provinsi_id').val();
		const kotaText = $('#kota_id option:selected').text() || $('#kota_id').val();
		const kecamatanText = $('#kecamatan_id option:selected').text() || $('#kecamatan_id').val();
		const kelurahanText = $('#kelurahan_id option:selected').text() || $('#kelurahan_id').val();
		addresses.push({
			jenis_alamat: 'KTP',
			alamat: $('#alamat_ktp').val(),
			rt: $('#rt_ktp').val(),
			rw: $('#rw_ktp').val(),
			provinsi: provinceText,
			kota: kotaText,
			kecamatan: kecamatanText,
			kelurahan: kelurahanText,
			kode_pos: $('#kode_pos').val()
		});
		if ($('#alamat_domisili').val()) {
			const domProvText = $('#provinsi_domisili option:selected').text() || $('#provinsi_domisili').val();
			const domKotaText = $('#kota_domisili option:selected').text() || $('#kota_domisili').val();
			const domKecText = $('#kecamatan_domisili option:selected').text() || $('#kecamatan_domisili').val();
			const domKelText = $('#kelurahan_domisili option:selected').text() || $('#kelurahan_domisili').val();
			addresses.push({
				jenis_alamat: 'DOMISILI',
				alamat: $('#alamat_domisili').val(),
				rt: $('#rt_domisili').val(),
				rw: $('#rw_domisili').val(),
				provinsi: domProvText,
				kota: domKotaText,
				kecamatan: domKecText,
				kelurahan: domKelText,
				kode_pos: $('#kode_pos_domisili').val()
			});
		}

		const bank = {
			bank_name: $('#bank_name').val(),
			nama_pemilik_rekening: $('#nama_pemilik_rekening').val(),
			nomor_rekening: $('#nomor_rekening').val()
		};

		const emergency = {
			nama: $('#nama_kontak_darurat').val(),
			relasi: $('#relasi_kontak_darurat').val(),
			hp1: $('#hp_kontak_darurat_1').val(),
			hp2: $('#hp_kontak_darurat_2').val()
		};

		const families = [];
		$('.keluarga-item').each(function () {
			families.push({
				hubungan: $(this).find('.input-keluarga-hubungan').val(),
				nama: $(this).find('.input-keluarga-nama').val(),
				tanggal_lahir: $(this).find('.input-keluarga-tanggal').val(),
				pekerjaan: $(this).find('.input-keluarga-pekerjaan').val(),
				nomor_hp: $(this).find('.input-keluarga-hp').val(),
				is_tanggungan: $(this).find('.input-keluarga-tanggungan').is(':checked')
			});
		});

		const educations = [];
		$('.pendidikan-item').each(function () {
			const index = $(this).data('index');
			educations.push({
				pendidikan_id: $(this).find('.input-pendidikan-id').val() || null,
				nama_instansi: $(this).find('.input-pendidikan-nama').val(),
				tahun_masuk: $(this).find('.input-pendidikan-masuk').val(),
				tahun_lulus: $(this).find('.input-pendidikan-lulus').val(),
				file_field: `pendidikan_file_${index}`
			});
		});

		const certifications = [];
		$('.sertifikasi-item').each(function () {
			const index = $(this).data('index');
			certifications.push({
				nama_sertifikasi: $(this).find('.input-sertifikasi-nama').val(),
				lembaga: $(this).find('.input-sertifikasi-lembaga').val(),
				nomor_sertifikat: $(this).find('.input-sertifikasi-nomor').val(),
				tanggal_terbit: $(this).find('.input-sertifikasi-terbit').val(),
				tanggal_kadaluarsa: $(this).find('.input-sertifikasi-expired').val(),
				file_field: `sertifikasi_file_${index}`
			});
		});

		const mcus = [];
		$('.mcu-item').each(function () {
			const index = $(this).data('index');
			mcus.push({
				hasil: $(this).find('.input-mcu-hasil').val(),
				fasilitas_kesehatan: $(this).find('.input-mcu-faskes').val(),
				tanggal: $(this).find('.input-mcu-tanggal').val(),
				file_field: `mcu_file_${index}`
			});
		});

		const vaksins = [];
		$('.vaksin-item').each(function () {
			vaksins.push({
				jenis_vaksin: $(this).find('.input-vaksin-jenis').val(),
				dosis_ke: parseInt($(this).find('.input-vaksin-dosis').val() || '0', 10),
				tanggal_vaksin: $(this).find('.input-vaksin-tanggal').val(),
				fasilitas_kesehatan: $(this).find('.input-vaksin-catatan').val(),
				catatan: $(this).find('.input-vaksin-catatan').val()
			});
		});

		const documents = includeDocuments ? DOC_REQUIREMENTS.map(doc => ({
			input_name: doc.input_name || doc.InputName,
			kode_dokumen: doc.kode_dokumen || doc.KodeDokumen,
			nama_dokumen: doc.nama_dokumen || doc.NamaDokumen
		})) : [];

		return {
			Profile: profile,
			Job: job,
			Addresses: addresses,
			Bank: bank,
			EmergencyContact: emergency,
			Families: families,
			Educations: educations,
			Certifications: certifications,
			Mcus: mcus,
			Vaccines: vaksins,
			Documents: documents,
			InviteToken: $('#invite_token').val() || (INVITE_CONTEXT.Token || null)
		};
	}

	function renderConfirmation(payload) {
		if (!payload) return;
		const items = [
			{ label: 'Indexim ID', value: payload.Profile?.indexim_id },
			{ label: 'Nama Lengkap', value: payload.Profile?.nama_lengkap },
			{ label: 'Perusahaan', value: $('#perusahaan_id option:selected').text() },
			{ label: 'Departemen', value: $('#departemen_id option:selected').text() },
			{ label: 'Posisi', value: payload.Job?.posisi_text },
			{ label: 'Tanggal Aktif', value: payload.Job?.tanggal_aktif },
			{ label: 'Kontak Darurat', value: `${payload.EmergencyContact?.nama || '-'} (${payload.EmergencyContact?.relasi || '-'})` }
		];
		const html = items.map(item => `
			<div class="d-flex justify-content-between border-bottom py-2">
				<span class="text-muted">${item.label}</span>
				<strong>${item.value || '-'}</strong>
			</div>`).join('');
		confirmationContainer.html(html);
	}

	async function submitWizard() {
		const payload = buildPayload(true);
		if (!payload || !payload.Profile || !payload.Profile.indexim_id) {
			Swal.fire('Gagal', 'Data belum lengkap.', 'warning');
			return;
		}
		$('#payload_json').val(JSON.stringify(payload));

		const formData = new FormData(form[0]);
		formData.append('__RequestVerificationToken', $('input[name="__RequestVerificationToken"]').val());
		btnNext.prop('disabled', true).text('Menyimpan...');
		try {
			const response = await fetch(form.attr('action'), {
				method: 'POST',
				body: formData
			});
			const result = await response.json();
			if (result.success) {
				Swal.fire('Sukses', result.message || 'Data karyawan berhasil disimpan.', 'success');
				form[0].reset();
				window.location.reload();
			} else {
				Swal.fire('Gagal', result.message || 'Gagal menyimpan data.', 'error');
			}
		} catch (error) {
			console.error(error);
			Swal.fire('Gagal', 'Terjadi kesalahan pada server.', 'error');
		} finally {
			btnNext.prop('disabled', false).text('Simpan Semua Data');
		}
	}

	$(document).ready(init);

})(jQuery);
