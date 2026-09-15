document.addEventListener("DOMContentLoaded", () => {
    const profilePictureInput = document.getElementById("profile-picture-input");

    if (profilePictureInput) {
        profilePictureInput.addEventListener("change", () => {
            if (profilePictureInput.files.length > 0) {
                profilePictureInput.form.submit();
            }
        });
    }

    PhoneNumber.initialize(
        document.getElementById("phone-number")
    );
});