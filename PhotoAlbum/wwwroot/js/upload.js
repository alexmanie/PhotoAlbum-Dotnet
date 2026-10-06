// Photo Album Upload JavaScript
(function () {
    'use strict';

    const dropZone = document.getElementById('drop-zone');
    const fileInput = document.getElementById('file-input');
    const uploadForm = document.getElementById('upload-form');
    const uploadFeedback = document.getElementById('upload-feedback');
    const uploadProgress = document.getElementById('upload-progress');
    const uploadSuccess = document.getElementById('upload-success');
    const uploadErrors = document.getElementById('upload-errors');
    const photoGallery = document.getElementById('photo-gallery');
    const uploadAlbumInput = document.getElementById('upload-album-id');
    const gallerySection = document.getElementById('gallery-section');
    // Album shown by a filtered gallery, or an empty string when all photos are shown
    const currentAlbumId = gallerySection ? gallerySection.dataset.albumId : undefined;

    if (!dropZone || !fileInput) {
        console.error('Required elements not found');
        return;
    }

    // Uploads are sent with fetch, so pressing Enter in the album field must not post the form
    uploadForm.addEventListener('submit', preventDefaults);

    // Click on drop zone to open file picker
    dropZone.addEventListener('click', () => {
        fileInput.click();
    });

    // Prevent default drag behaviors
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    // Highlight drop zone when dragging over it
    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => {
            dropZone.classList.add('drop-zone-highlight');
        }, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => {
            dropZone.classList.remove('drop-zone-highlight');
        }, false);
    });

    // Handle dropped files
    dropZone.addEventListener('drop', (e) => {
        const dt = e.dataTransfer;
        const files = dt.files;
        handleFiles(files);
    }, false);

    // Handle file input change
    fileInput.addEventListener('change', (e) => {
        handleFiles(e.target.files);
    });

    function handleFiles(files) {
        if (!files || files.length === 0) {
            return;
        }

        // A number input reports an empty value for unparsable text, so check validity before reading it
        if (!uploadAlbumInput.checkValidity()) {
            showErrors([`Album ID: ${uploadAlbumInput.validationMessage}`]);
            fileInput.value = '';
            return;
        }

        // Normalizes entries such as 007 or 1e3 to the integer the server expects
        const albumId = uploadAlbumInput.value === '' ? '' : String(uploadAlbumInput.valueAsNumber);

        // Client-side validation
        const validFiles = [];
        const errors = [];
        const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
        const maxSize = 10 * 1024 * 1024; // 10MB

        Array.from(files).forEach(file => {
            if (!allowedTypes.includes(file.type)) {
                errors.push(`${file.name}: File type not supported. Please upload JPEG, PNG, GIF, or WebP images.`);
            } else if (file.size > maxSize) {
                errors.push(`${file.name}: File size exceeds 10MB limit.`);
            } else {
                validFiles.push(file);
            }
        });

        if (errors.length > 0) {
            showErrors(errors);
        }

        if (validFiles.length > 0) {
            uploadFiles(validFiles, albumId);
        }
    }

    async function uploadFiles(files, albumId) {
        // Show progress
        uploadFeedback.classList.remove('d-none');
        uploadProgress.classList.remove('d-none');
        uploadSuccess.classList.add('d-none');
        uploadErrors.classList.add('d-none');

        const formData = new FormData();
        files.forEach(file => {
            formData.append('files', file);
        });

        if (albumId) {
            formData.append('albumId', albumId);
        }

        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            formData.append('__RequestVerificationToken', token.value);
        }

        try {
            const response = await fetch('/Index?handler=Upload', {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': token ? token.value : ''
                }
            });

            uploadProgress.classList.add('d-none');

            if (response.ok) {
                const result = await response.json();

                if (result.uploadedPhotos && result.uploadedPhotos.length > 0) {
                    const albumText = albumId ? ` to album ${albumId}` : '';
                    showSuccess(`Successfully uploaded ${result.uploadedPhotos.length} photo(s)${albumText}!`);

                    // A filtered gallery only shows uploads that belong to its album
                    const visiblePhotos = currentAlbumId
                        ? result.uploadedPhotos.filter(photo => String(photo.albumId) === currentAlbumId)
                        : result.uploadedPhotos;
                    if (visiblePhotos.length > 0) {
                        displayNewPhotos(visiblePhotos);
                    }
                }

                if (result.failedUploads && result.failedUploads.length > 0) {
                    const errorMessages = result.failedUploads.map(f => `${f.fileName}: ${f.error}`);
                    showErrors(errorMessages);
                }
            } else {
                const result = await response.json().catch(() => null);
                showErrors([result && result.error ? result.error : 'Upload failed. Please try again.']);
            }
        } catch (error) {
            uploadProgress.classList.add('d-none');
            console.error('Upload error:', error);
            showErrors(['An error occurred during upload. Please try again.']);
        } finally {
            // Allows the same files to be selected again after success or failure
            fileInput.value = '';
        }
    }

    function displayNewPhotos(photos) {
        // Remove "no photos" message if it exists
        const alertInfo = document.querySelector('#gallery-section .alert-info');
        if (alertInfo) {
            alertInfo.remove();
        }

        // Get the current gallery element (may have been created dynamically)
        let galleryElement = document.getElementById('photo-gallery');

        // Create gallery if it doesn't exist
        if (!galleryElement) {
            const gallerySection = document.getElementById('gallery-section');
            galleryElement = document.createElement('div');
            galleryElement.className = 'row';
            galleryElement.id = 'photo-gallery';
            gallerySection.appendChild(galleryElement);
        }

        // Add photos to the beginning of the gallery
        photos.forEach(photo => {
            const photoCard = createPhotoCard(photo);
            galleryElement.insertAdjacentHTML('afterbegin', photoCard);
        });
    }

    function createPhotoCard(photo) {
        const uploadDate = new Date(photo.uploadedAt);
        const formattedDate = uploadDate.toLocaleString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
            hour: 'numeric',
            minute: '2-digit',
            hour12: true
        });

        const dimensions = (photo.width && photo.height)
            ? `<span> • ${photo.width} x ${photo.height}</span>`
            : '';

        // Use indirect photo URL
        const photoUrl = `/photo/${photo.id}`;
        const detailUrl = currentAlbumId
            ? `/Detail/${photo.id}?albumId=${encodeURIComponent(currentAlbumId)}`
            : `/Detail/${photo.id}`;
        const fileName = escapeHtml(photo.originalFileName);

        return `
            <div class="col-12 col-sm-6 col-md-4 col-lg-3 mb-4">
                <div class="card photo-card h-100">
                    <a href="${detailUrl}" class="photo-link">
                        <img src="${photoUrl}" class="card-img-top" alt="${fileName}" loading="lazy">
                    </a>
                    <div class="card-body">
                        <p class="card-text text-truncate" title="${fileName}">
                            <small><a href="${detailUrl}" class="text-decoration-none text-dark">${fileName}</a></small>
                        </p>
                        <p class="card-text">
                            <small class="text-muted">${formattedDate}</small>
                        </p>
                        <p class="card-text">
                            <small class="text-muted">
                                ${Math.round(photo.fileSize / 1024)} KB${dimensions}
                            </small>
                        </p>
                    </div>
                </div>
            </div>
        `;
    }

    function showSuccess(message) {
        uploadSuccess.textContent = message;
        uploadSuccess.classList.remove('d-none');

        // Auto-hide after 5 seconds
        setTimeout(() => {
            uploadSuccess.classList.add('d-none');
        }, 5000);
    }

    function showErrors(errors) {
        uploadErrors.innerHTML = '<strong>Upload errors:</strong><ul class="mb-0 mt-2">' +
            errors.map(e => `<li>${escapeHtml(e)}</li>`).join('') +
            '</ul>';
        uploadErrors.classList.remove('d-none');
        uploadFeedback.classList.remove('d-none');
    }

    // File names and messages can contain markup, so encode them before inserting HTML
    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }
})();
