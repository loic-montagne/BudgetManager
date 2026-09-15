document.addEventListener("DOMContentLoaded", () => {
    document
        .querySelectorAll("[data-password-requirements]")
        .forEach(passwordInput => {
            const form = passwordInput.closest("form");

            if (!form) {
                return;
            }

            const requirements = form.querySelector(".password-requirements");

            if (!requirements) {
                return;
            }

            const requiredLength =
                Number(requirements.dataset.requiredLength);

            const requiredUniqueChars =
                Number(requirements.dataset.requiredUniqueChars);

            const rules = {
                length: password =>
                    password.length >= requiredLength,

                uppercase: password =>
                    /[A-Z]/.test(password),

                lowercase: password =>
                    /[a-z]/.test(password),

                digit: password =>
                    /\d/.test(password),

                "non-alphanumeric": password =>
                    /[^A-Za-z0-9]/.test(password),

                unique: password =>
                    new Set(password).size >= requiredUniqueChars
            };

            const updateRequirements = () => {
                const password = passwordInput.value;

                requirements
                    .querySelectorAll("[data-password-rule]")
                    .forEach(ruleElement => {
                        const ruleName =
                            ruleElement.dataset.passwordRule;

                        const rule = rules[ruleName];

                        if (!rule) {
                            return;
                        }

                        const isValid = rule(password);

                        ruleElement.classList.toggle(
                            "valid",
                            isValid
                        );

                        const icon =
                            ruleElement.querySelector("i");

                        if (icon) {
                            icon.classList.toggle(
                                "fa-regular",
                                !isValid
                            );

                            icon.classList.toggle(
                                "fa-circle",
                                !isValid
                            );

                            icon.classList.toggle(
                                "fa-solid",
                                isValid
                            );

                            icon.classList.toggle(
                                "fa-circle-check",
                                isValid
                            );
                        }
                    });
            };

            passwordInput.addEventListener(
                "input",
                updateRequirements
            );

            updateRequirements();
        });
});