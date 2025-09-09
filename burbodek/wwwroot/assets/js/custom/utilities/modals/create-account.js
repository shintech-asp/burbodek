"use strict";

var KTCreateAccount = function () {
    var modalEl, stepperEl, formEl, submitBtn, nextBtn, stepper, validations = [];

    return {
        init: function () {
            // Modal
            modalEl = document.querySelector("#kt_modal_create_account");
            if (modalEl) {
                new bootstrap.Modal(modalEl);
            }

            // Stepper
            stepperEl = document.querySelector("#kt_create_account_stepper");
            if (!stepperEl) return;

            formEl = stepperEl.querySelector("#kt_create_account_form");
            submitBtn = stepperEl.querySelector('[data-kt-stepper-action="submit"]');
            nextBtn = stepperEl.querySelector('[data-kt-stepper-action="next"]');
            stepper = new KTStepper(stepperEl);

            // Step change handler
            stepper.on("kt.stepper.changed", function () {
                let currentStep = stepper.getCurrentStepIndex();

                if (currentStep === 5) {
                    // Step 5: show submit, hide next
                    submitBtn.classList.remove("d-none");
                    submitBtn.classList.add("d-inline-block");
                    nextBtn.classList.add("d-none");
                } else if (currentStep === 6) {
                    // Step 6: hide everything (Done)
                    submitBtn.classList.add("d-none");
                    nextBtn.classList.add("d-none");
                } else {
                    // Other steps: hide submit, show next
                    submitBtn.classList.add("d-none");
                    submitBtn.classList.remove("d-inline-block");
                    nextBtn.classList.remove("d-none");
                }
            });

            // Next step handler
            stepper.on("kt.stepper.next", function (step) {
                console.log("stepper.next");
                var validator = validations[step.getCurrentStepIndex() - 1];
                if (validator) {
                    validator.validate().then(function (status) {
                        if (status === "Valid") {
                            step.goNext();
                            KTUtil.scrollTop();
                        } else {
                            Swal.fire({
                                text: "Sorry, looks like there are some errors detected, please try again.",
                                icon: "error",
                                buttonsStyling: false,
                                confirmButtonText: "Ok, got it!",
                                customClass: { confirmButton: "btn btn-light" }
                            }).then(function () {
                                KTUtil.scrollTop();
                            });
                        }
                    });
                } else {
                    step.goNext();
                    KTUtil.scrollTop();
                }
            });

            // Previous step handler
            stepper.on("kt.stepper.previous", function (step) {
                console.log("stepper.previous");
                step.goPrevious();
                KTUtil.scrollTop();
            });

            // Step 1 validation
            validations.push(FormValidation.formValidation(formEl, {
                fields: {
                    account_type: {
                        validators: { notEmpty: { message: "Account type is required" } }
                    }
                },
                plugins: {
                    trigger: new FormValidation.plugins.Trigger(),
                    bootstrap: new FormValidation.plugins.Bootstrap5({
                        rowSelector: ".fv-row",
                        eleInvalidClass: "",
                        eleValidClass: ""
                    })
                }
            }));

            // Step 2 validation
            validations.push(FormValidation.formValidation(formEl, {
                fields: {
                    account_team_size: {
                        validators: { notEmpty: { message: "Team size is required" } }
                    },
                    account_name: {
                        validators: { notEmpty: { message: "Account name is required" } }
                    },
                    account_plan: {
                        validators: { notEmpty: { message: "Account plan is required" } }
                    }
                },
                plugins: {
                    trigger: new FormValidation.plugins.Trigger(),
                    bootstrap: new FormValidation.plugins.Bootstrap5({
                        rowSelector: ".fv-row",
                        eleInvalidClass: "",
                        eleValidClass: ""
                    })
                }
            }));

            // Step 3 validation
            validations.push(FormValidation.formValidation(formEl, {
                fields: {
                    business_name: {
                        validators: { notEmpty: { message: "Business name is required" } }
                    },
                    business_descriptor: {
                        validators: { notEmpty: { message: "Business descriptor is required" } }
                    },
                    business_type: {
                        validators: { notEmpty: { message: "Business type is required" } }
                    },
                    business_email: {
                        validators: {
                            notEmpty: { message: "Business email is required" },
                            emailAddress: { message: "The value is not a valid email address" }
                        }
                    }
                },
                plugins: {
                    trigger: new FormValidation.plugins.Trigger(),
                    bootstrap: new FormValidation.plugins.Bootstrap5({
                        rowSelector: ".fv-row",
                        eleInvalidClass: "",
                        eleValidClass: ""
                    })
                }
            }));

            // Step 4 validation
            validations.push(FormValidation.formValidation(formEl, {
                fields: {
                    card_name: {
                        validators: { notEmpty: { message: "Name on card is required" } }
                    },
                    card_number: {
                        validators: {
                            notEmpty: { message: "Card number is required" },
                            creditCard: { message: "Card number is not valid" }
                        }
                    },
                    card_expiry_month: {
                        validators: { notEmpty: { message: "Month is required" } }
                    },
                    card_expiry_year: {
                        validators: { notEmpty: { message: "Year is required" } }
                    },
                    card_cvv: {
                        validators: {
                            notEmpty: { message: "CVV is required" },
                            digits: { message: "CVV must contain only digits" },
                            stringLength: {
                                min: 3,
                                max: 4,
                                message: "CVV must contain 3 to 4 digits only"
                            }
                        }
                    }
                },
                plugins: {
                    trigger: new FormValidation.plugins.Trigger(),
                    bootstrap: new FormValidation.plugins.Bootstrap5({
                        rowSelector: ".fv-row",
                        eleInvalidClass: "",
                        eleValidClass: ""
                    })
                }
            }));

            // Step 5 validation (new)
            validations.push(FormValidation.formValidation(formEl, {
                fields: {
                    extra_info: {
                        validators: { notEmpty: { message: "Extra information is required" } }
                    }
                },
                plugins: {
                    trigger: new FormValidation.plugins.Trigger(),
                    bootstrap: new FormValidation.plugins.Bootstrap5({
                        rowSelector: ".fv-row",
                        eleInvalidClass: "",
                        eleValidClass: ""
                    })
                }
            }));

            // Submit button handler (Step 5)
            submitBtn.addEventListener("click", function (e) {
                var validator = validations[stepper.getCurrentStepIndex() - 1];
                if (validator) {
                    validator.validate().then(function (status) {
                        if (status === "Valid") {
                            e.preventDefault();
                            submitBtn.disabled = true;
                            submitBtn.setAttribute("data-kt-indicator", "on");

                            setTimeout(function () {
                                submitBtn.removeAttribute("data-kt-indicator");
                                submitBtn.disabled = false;
                                stepper.goNext(); // go to step 6
                            }, 2000);
                        } else {
                            Swal.fire({
                                text: "Sorry, looks like there are some errors detected, please try again.",
                                icon: "error",
                                buttonsStyling: false,
                                confirmButtonText: "Ok, got it!",
                                customClass: { confirmButton: "btn btn-light" }
                            }).then(function () {
                                KTUtil.scrollTop();
                            });
                        }
                    });
                }
            });

            // Dynamic revalidation
            $(formEl.querySelector('[name="card_expiry_month"]')).on("change", function () {
                validations[3].revalidateField("card_expiry_month");
            });

            $(formEl.querySelector('[name="card_expiry_year"]')).on("change", function () {
                validations[3].revalidateField("card_expiry_year");
            });

            $(formEl.querySelector('[name="business_type"]')).on("change", function () {
                validations[2].revalidateField("business_type");
            });
        }
    };
}();

KTUtil.onDOMContentLoaded(function () {
    KTCreateAccount.init();
});
