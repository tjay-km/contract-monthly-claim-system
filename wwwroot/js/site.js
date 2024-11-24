// Hamburger menu
document.addEventListener('DOMContentLoaded', function() {
    const mobileMenuButton = document.getElementById('mobile-menu-button');
    const mobileMenu = document.getElementById('mobile-menu');

    mobileMenuButton.addEventListener('click', function() {
        mobileMenu.classList.toggle('hidden');
    });
});

// Claim form validation
function initClaimFormValidation() {
    const form = document.getElementById('claimForm');
    const hoursWorked = document.getElementById('hoursWorked');
    const hourlyRate = document.getElementById('hourlyRate');
    const totalAmountDisplay = document.getElementById('totalAmountDisplay');
    const totalAmountValue = document.getElementById('totalAmountValue');
    const supportingDocument = document.getElementById('supportingDocument');
    const departmentSelect = document.getElementById('DepartmentId');
    const moduleSelect = document.getElementById('ModuleId');

    // Load modules when department changes
    if (departmentSelect) {
        departmentSelect.addEventListener('change', function() {
            const departmentId = this.value;
            if (departmentId) {
                // Add a loading indicator
                moduleSelect.innerHTML = '<option value="">Loading modules...</option>';
                
                fetch(`/api/Claim/GetModules/${departmentId}`)
                    .then(response => {
                        if (!response.ok) {
                            throw new Error('Network response was not ok');
                        }
                        return response.json();
                    })
                    .then(data => {
                        moduleSelect.innerHTML = '<option value="">Select Module</option>';
                        data.forEach(module => {
                            const option = document.createElement('option');
                            option.value = module.id;
                            option.textContent = `${module.code} - ${module.name}`;
                            moduleSelect.appendChild(option);
                        });
                        moduleSelect.classList.remove('is-valid', 'is-invalid');
                    })
                    .catch(error => {
                        console.error('Error loading modules:', error);
                        moduleSelect.innerHTML = '<option value="">Error loading modules</option>';
                    });
            } else {
                moduleSelect.innerHTML = '<option value="">Select Module</option>';
                moduleSelect.classList.remove('is-valid', 'is-invalid');
            }
        });
    }

    // Function to format currency
    function formatCurrency(amount) {
        return new Intl.NumberFormat('en-ZA', {
            style: 'currency',
            currency: 'ZAR'
        }).format(amount);
    }

    // Function to calculate and display total amount
    function calculateTotal() {
        if (hoursWorked.value && hourlyRate.value) {
            const hours = parseFloat(hoursWorked.value);
            const rate = parseFloat(hourlyRate.value);
            const total = hours * rate;

            totalAmountDisplay.value = formatCurrency(total);
            totalAmountValue.value = total;

            // Validate total amount
            if (total > 3900000) {
                totalAmountDisplay.classList.add('is-invalid');
                totalAmountDisplay.classList.remove('is-valid');
            } else {
                totalAmountDisplay.classList.add('is-valid');
                totalAmountDisplay.classList.remove('is-invalid');
            }
        } else {
            totalAmountDisplay.value = '';
            totalAmountValue.value = '';
            totalAmountDisplay.classList.remove('is-valid', 'is-invalid');
        }
    }

    // Event listeners for real-time calculation
    if (hoursWorked && hourlyRate) {
        hoursWorked.addEventListener('input', calculateTotal);
        hourlyRate.addEventListener('input', calculateTotal);
    }

    // File validation
    if (supportingDocument) {
        supportingDocument.addEventListener('change', function(e) {
            const file = e.target.files[0];
            const validTypes = ['application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
            const maxSize = 5 * 1024 * 1024; // 5MB in bytes

            if (file) {
                const isValidType = validTypes.includes(file.type);
                const isValidSize = file.size <= maxSize;

                if (isValidType && isValidSize) {
                    this.classList.add('is-valid');
                    this.classList.remove('is-invalid');
                } else {
                    this.classList.add('is-invalid');
                    this.classList.remove('is-valid');
                }
            }
        });
    }

    // Form submission validation
    if (form) {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }

            const total = parseFloat(totalAmountValue.value);
            if (total > 3900000) {
                event.preventDefault();
                event.stopPropagation();
                alert('Total amount cannot exceed R3,900,000');
            }

            form.classList.add('was-validated');
        });
    }
}

// Initialize all validations when the document is ready
document.addEventListener('DOMContentLoaded', () => {
    initClaimFormValidation();
});
