document.getElementById('signUpForm').addEventListener('submit', function (event) {
    // Clear previous error messages
    const errorMessage = document.getElementById('error-message');
    errorMessage.textContent = '';

    // Get form values
    const name = document.getElementsByName('Name')[0].value;
    const emailOrUsername = document.getElementsByName('EmailOrUsername')[0].value;
    const password = document.getElementsByName('Password')[0].value;
    const confirmPassword = document.getElementsByName('confirmPassword')[0].value;

    // Email and Username Validation Regex
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const usernameRegex = /^[a-zA-Z0-9._-]{3,}$/;  // Allows usernames with at least 3 characters

    // Validate Name
    if (!name) {
        errorMessage.textContent = 'Name is required.';
        event.preventDefault(); // Prevent form submission
        return;
    }

    // Validate Email or Username
    if (!emailRegex.test(emailOrUsername) && !usernameRegex.test(emailOrUsername)) {
        errorMessage.textContent = 'Please enter a valid email address or username.';
        event.preventDefault(); // Prevent form submission
        return;
    }

    // Validate Password Length
    if (password.length < 8) {
        errorMessage.textContent = 'Password must be at least 8 characters long.';
        event.preventDefault(); // Prevent form submission
        return;
    }

    // Validate Password Match
    if (password !== confirmPassword) {
        errorMessage.textContent = 'Passwords do not match.';
        event.preventDefault(); // Prevent form submission
        return;
    }
});
