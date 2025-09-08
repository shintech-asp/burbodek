"use strict";

var KTCreateAccount = function () {
    var e, t, i, o, a, r, s = [];

    return {
        init: function () {
            // Modal
            e = document.querySelector("#kt_modal_create_account");
            if (e) {
                new bootstrap.Modal(e);
            }

            // Stepper
            t = document.querySelector("#kt_create_account_stepper");
            if (!t) return;

            i = t.querySelector("#kt_create_account_form");
            o = t.querySelector('[data-kt-stepper-action="submit"]');
            a = t.querySelector('[data-kt-stepper-action="next"]');
            r = new KTStepper(t);

            // Step change handler
            r.on("kt.stepper.changed", function () {
                if (r.getCurrentStepIndex() === 5) {
                    // Step 5: show submit, hide next
                    o.classList.remove("d-none");
                    o.classList.add("d-inline-block");
                    a.classList.add("d-none");
                } else if (r.getCurrentStepIndex() === 6) {
                    // Step 6: hide everything (Done)
                    o.classList.add("d-none");
                    a.classList.add("d-none");
                } else {
                    // Other steps: show next, hide submit
                    o.classList.remove("d-inline-block");
                    o.classList.remove("d-none");
                    a.classList.remove("d-none");
                }
            });

            // Next step handler
            r.on("kt.stepper.next", function (e) {
                console.log("stepper.next");
                var t = s[e.getCurrentStepIndex() - 1];
                if (t) {
                    t.validate().then(function (status) {
                        console.log("validated!");
                        if (status === "Valid") {
                            e.goNext();
                            KTUtil.scrollTop();
                        } else {
                            Swal.fire({
                                text: "Sorry, looks like there are some errors detected, please try again.",
                                icon: "error",
                                buttonsStyling: !1,
                                confirmButtonText: "Ok, got it!",
                                customClass: { confirmButton: "btn btn-light" }
                            }).then(function () {
                                KTUtil.scrollTop();
                            });
                        }
                    });
                } else {
                    e.goNext();
                    KTUtil.scrollTop();
                }
            });

            // Previous step handler
            r.on("kt.stepper.previous", function (e) {
                console.log("stepper.previous");
                e.goPrevious();
                KTUtil.scrollTop();
            });

            // Step 1 validation
            s.push(FormValidation.formValidation(i, {
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
            s.push(FormValidation.formValidation(i, {
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
            s.push(FormValidation.formValidation(i, {
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
            s.push(FormValidation.formValidation(i, {
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

            // Step 5 validation (NEW STEP)
            s.push(FormValidation.formValidation(i, {
                fields: {
                    // Example field, replace with your step 5 fields
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

            // Submit button handler
            o.addEventListener("click", function (e) {
                s[4].validate().then(function (status) {
                    console.log("validated!");
                    if (status === "Valid") {
                        e.preventDefault();
                        o.disabled = !0;
                        o.setAttribute("data-kt-indicator", "on");

                        setTimeout(function () {
                            o.removeAttribute("data-kt-indicator");
                            o.disabled = !1;
                            r.goNext();
                        }, 2000);
                    } else {
                        Swal.fire({
                            text: "Sorry, looks like there are some errors detected, please try again.",
                            icon: "error",
                            buttonsStyling: !1,
                            confirmButtonText: "Ok, got it!",
                            customClass: { confirmButton: "btn btn-light" }
                        }).then(function () {
                            KTUtil.scrollTop();
                        });
                    }
                });
            });

            // Dynamic revalidation
            $(i.querySelector('[name="card_expiry_month"]')).on("change", function () {
                s[3].revalidateField("card_expiry_month");
            });

            $(i.querySelector('[name="card_expiry_year"]')).on("change", function () {
                s[3].revalidateField("card_expiry_year");
            });

            $(i.querySelector('[name="business_type"]')).on("change", function () {
                s[2].revalidateField("business_type");
            });
        }
    };
}();

KTUtil.onDOMContentLoaded(function () {
    KTCreateAccount.init();
});
