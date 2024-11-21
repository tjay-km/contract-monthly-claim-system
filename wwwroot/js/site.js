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
    const claimForm = document.getElementById('claimForm');
    if (!claimForm) return;

    const hoursWorked = document.getElementById('hoursWorked');
    const hourlyRate = document.getElementById('hourlyRate');
    const totalAmount = document.getElementById('totalAmount');
    const additionalNotes = document.getElementById('AdditionalNotes');
    const fileInput = document.querySelector('input[type="file"]');

    function updateTotal() {
        const hours = parseFloat(hoursWorked.value) || 0;
        const rate = parseFloat(hourlyRate.value) || 0;
        const total = (hours * rate).toFixed(2);
        totalAmount.value = total;

        // Visual feedback
        if (total > 0) {
            totalAmount.classList.add('is-valid');
            totalAmount.classList.remove('is-invalid');
        } else {
            totalAmount.classList.add('is-invalid');
            totalAmount.classList.remove('is-valid');
        }
    }

    function validateHours() {
        const hours = parseFloat(hoursWorked.value);
        const isValid = hours >= 0.5 && hours <= 195;
        
        hoursWorked.classList.toggle('is-valid', isValid);
        hoursWorked.classList.toggle('is-invalid', !isValid);
        
        return isValid;
    }

    function validateRate() {
        const rate = parseFloat(hourlyRate.value);
        const isValid = rate >= 1 && rate <= 20000;
        
        hourlyRate.classList.toggle('is-valid', isValid);
        hourlyRate.classList.toggle('is-invalid', !isValid);
        
        return isValid;
    }

    function validateNotes() {
        const notes = additionalNotes.value.trim();
        const isValid = notes.length >= 10 && notes.length <= 500;
        
        additionalNotes.classList.toggle('is-valid', isValid);
        additionalNotes.classList.toggle('is-invalid', !isValid);
        
        return isValid;
    }

    function validateFile() {
        if (!fileInput.files || fileInput.files.length === 0) {
            return false;
        }

        const file = fileInput.files[0];
        const allowedTypes = ['.pdf', '.doc', '.docx'];
        const extension = '.' + file.name.split('.').pop().toLowerCase();
        const isValid = allowedTypes.includes(extension) && file.size <= 5 * 1024 * 1024;

        fileInput.classList.toggle('is-valid', isValid);
        fileInput.classList.toggle('is-invalid', !isValid);

        return isValid;
    }

    // Event listeners
    hoursWorked.addEventListener('input', () => {
        validateHours();
        updateTotal();
    });

    hourlyRate.addEventListener('input', () => {
        validateRate();
        updateTotal();
    });

    additionalNotes.addEventListener('input', validateNotes);
    fileInput.addEventListener('change', validateFile);

    // Form submission
    claimForm.addEventListener('submit', (e) => {
        if (!validateHours() || !validateRate() || !validateNotes() || !validateFile()) {
            e.preventDefault();
            e.stopPropagation();
        }
        claimForm.classList.add('was-validated');
    });

    // Initialize validation state
    updateTotal();
}

// Initialize all validations when the document is ready
document.addEventListener('DOMContentLoaded', () => {
    initClaimFormValidation();
});
