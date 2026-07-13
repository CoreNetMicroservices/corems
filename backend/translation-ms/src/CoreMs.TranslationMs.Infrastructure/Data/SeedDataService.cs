using CoreMs.TranslationMs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreMs.TranslationMs.Infrastructure.Data;

/// <summary>
/// Seeds development/staging translation bundles matching the Java reference implementation.
/// </summary>
public class SeedDataService
{
    private readonly TranslationMsDbContext _context;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(TranslationMsDbContext context, ILogger<SeedDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _context.Set<TranslationBundleEntity>().AnyAsync(t => t.Realm == "corems" && t.Lang == "en"))
        {
            _logger.LogInformation("Translation seed data already exists — skipping");
            return;
        }

        _logger.LogInformation("Seeding translation bundles...");
        _context.Set<TranslationBundleEntity>().AddRange(CreateBundles());
        await _context.SaveChangesAsync();
        _logger.LogInformation("Translation seed data complete — 2 bundles created (corems/en, corems/no)");
    }

    private static List<TranslationBundleEntity> CreateBundles() =>
    [
        new TranslationBundleEntity
        {
            Realm = "corems",
            Lang = "en",
            Data = EnglishTranslations(),
            UpdatedAt = DateTime.UtcNow
        },
        new TranslationBundleEntity
        {
            Realm = "corems",
            Lang = "no",
            Data = NorwegianTranslations(),
            UpdatedAt = DateTime.UtcNow
        }
    ];

    private static Dictionary<string, string> EnglishTranslations() => new()
    {
        ["app.name"] = "CoreMS",
        ["app.title"] = "Core Microservices",
        ["common.actions"] = "Actions",
        ["common.add"] = "Add",
        ["common.cancel"] = "Cancel",
        ["common.close"] = "Close",
        ["common.creating"] = "Creating...",
        ["common.delete"] = "Delete",
        ["common.edit"] = "Edit",
        ["common.loading"] = "Loading...",
        ["common.save"] = "Save",
        ["common.saveChanges"] = "Save Changes",
        ["common.saving"] = "Saving...",
        ["common.sending"] = "Sending...",
        ["common.verifying"] = "Verifying...",
        ["common.or"] = "or",
        ["common.optional"] = "Optional",
        ["nav.documents"] = "Documents",
        ["nav.translations"] = "Translations",
        ["nav.users"] = "Users",
        ["form.confirmPassword"] = "Confirm Password",
        ["form.email"] = "Email",
        ["form.enterEmail"] = "Enter email",
        ["form.firstName"] = "First Name",
        ["form.lastName"] = "Last Name",
        ["form.password"] = "Password",
        ["form.phoneNumber"] = "Phone Number",
        ["form.verificationCode"] = "Verification Code",
        ["validation.emailRequired"] = "Email is required",
        ["validation.firstNameRequired"] = "First name is required",
        ["validation.lastNameRequired"] = "Last name is required",
        ["validation.invalidEmail"] = "Invalid email format",
        ["auth.signIn"] = "Sign In",
        ["auth.register"] = "Register new account",
        ["auth.forgotPassword"] = "Forgot password?",
        ["auth.resetPassword"] = "Reset Password",
        ["auth.backToLogin"] = "Back to Login",
        ["auth.verifyEmail"] = "Verify Email",
        ["auth.verifyPhone"] = "Verify Phone Number",
        ["auth.emailVerifiedSuccess"] = "Your email has been successfully verified!",
        ["auth.resendVerification"] = "Resend Verification Email",
        ["auth.resendVerificationSMS"] = "Resend Verification SMS",
        ["password.changePassword"] = "Change Password",
        ["profile.verified"] = "Verified",
        ["profile.unverified"] = "Unverified",
        ["profile.verify"] = "Verify",
        ["profile.backToProfile"] = "Back to Profile",
        ["user.allUsers"] = "All Users",
        ["user.addNewUser"] = "Add New User",
        ["user.editUser"] = "Edit User",
        ["user.createUser"] = "Create User",
        ["user.name"] = "Name",
        ["user.email"] = "Email",
        ["user.provider"] = "Provider",
        ["user.localProvider"] = "Local",
        ["user.allProviders"] = "All Providers",
        ["user.roles"] = "Roles",
        ["user.addRole"] = "Add Role",
        ["user.noRolesAssigned"] = "No roles assigned",
        ["user.lastLogin"] = "Last Login",
        ["user.accountInfo"] = "Account Info",
        ["user.userId"] = "User ID",
        ["user.created"] = "Created",
        ["user.notFound"] = "User not found",
        ["user.backToUsers"] = "Back to Users",
        ["user.searchPlaceholder"] = "Search users by name or email...",
        ["user.updateSuccess"] = "User information has been updated successfully.",
        ["user.updateFailed"] = "Failed to update user information.",
        ["user.defaultAvatarGenerated"] = "Avatar will be generated based on initials",
        ["document.documents"] = "Documents",
        ["document.description"] = "Description",
        ["document.tags"] = "Tags",
        ["document.generateLink"] = "Generate Document Link",
        ["document.deletePermanently"] = "Delete Permanently",
        ["translation.newTranslation"] = "New Translation",
        ["translation.yesDelete"] = "Yes, Delete"
    };

    private static Dictionary<string, string> NorwegianTranslations() => new()
    {
        ["app.name"] = "CoreMS",
        ["app.title"] = "Core Microservices",
        ["common.actions"] = "Handlinger",
        ["common.add"] = "Legg til",
        ["common.cancel"] = "Avbryt",
        ["common.close"] = "Lukk",
        ["common.creating"] = "Oppretter...",
        ["common.delete"] = "Slett",
        ["common.edit"] = "Rediger",
        ["common.loading"] = "Laster...",
        ["common.save"] = "Lagre",
        ["common.saveChanges"] = "Lagre endringer",
        ["common.saving"] = "Lagrer...",
        ["common.sending"] = "Sender...",
        ["common.verifying"] = "Verifiserer...",
        ["common.or"] = "eller",
        ["common.optional"] = "Valgfritt",
        ["nav.documents"] = "Dokumenter",
        ["nav.translations"] = "Oversettelser",
        ["nav.users"] = "Brukere",
        ["form.confirmPassword"] = "Bekreft passord",
        ["form.email"] = "E-post",
        ["form.enterEmail"] = "Skriv inn e-post",
        ["form.firstName"] = "Fornavn",
        ["form.lastName"] = "Etternavn",
        ["form.password"] = "Passord",
        ["form.phoneNumber"] = "Telefonnummer",
        ["form.verificationCode"] = "Verifiseringskode",
        ["validation.emailRequired"] = "E-post er påkrevd",
        ["validation.firstNameRequired"] = "Fornavn er påkrevd",
        ["validation.lastNameRequired"] = "Etternavn er påkrevd",
        ["validation.invalidEmail"] = "Ugyldig e-postformat",
        ["auth.signIn"] = "Logg inn",
        ["auth.register"] = "Registrer ny konto",
        ["auth.forgotPassword"] = "Glemt passord?",
        ["auth.resetPassword"] = "Tilbakestill passord",
        ["auth.backToLogin"] = "Tilbake til innlogging",
        ["auth.verifyEmail"] = "Verifiser e-post",
        ["auth.verifyPhone"] = "Verifiser telefonnummer",
        ["auth.emailVerifiedSuccess"] = "E-posten din er verifisert!",
        ["auth.resendVerification"] = "Send verifiserings-e-post på nytt",
        ["auth.resendVerificationSMS"] = "Send verifiserings-SMS på nytt",
        ["password.changePassword"] = "Endre passord",
        ["profile.verified"] = "Verifisert",
        ["profile.unverified"] = "Ikke verifisert",
        ["profile.verify"] = "Verifiser",
        ["profile.backToProfile"] = "Tilbake til profil",
        ["user.allUsers"] = "Alle brukere",
        ["user.addNewUser"] = "Legg til ny bruker",
        ["user.editUser"] = "Rediger bruker",
        ["user.createUser"] = "Opprett bruker",
        ["user.name"] = "Navn",
        ["user.email"] = "E-post",
        ["user.provider"] = "Leverandør",
        ["user.localProvider"] = "Lokal",
        ["user.allProviders"] = "Alle leverandører",
        ["user.roles"] = "Roller",
        ["user.addRole"] = "Legg til rolle",
        ["user.noRolesAssigned"] = "Ingen roller tildelt",
        ["user.lastLogin"] = "Siste innlogging",
        ["user.accountInfo"] = "Kontoinformasjon",
        ["user.userId"] = "Bruker-ID",
        ["user.created"] = "Opprettet",
        ["user.notFound"] = "Bruker ikke funnet",
        ["user.backToUsers"] = "Tilbake til brukere",
        ["user.searchPlaceholder"] = "Søk etter brukere etter navn eller e-post...",
        ["user.updateSuccess"] = "Brukerinformasjon er oppdatert.",
        ["user.updateFailed"] = "Kunne ikke oppdatere brukerinformasjon.",
        ["user.defaultAvatarGenerated"] = "Avatar vil bli generert basert på initialer",
        ["document.documents"] = "Dokumenter",
        ["document.description"] = "Beskrivelse",
        ["document.tags"] = "Tagger",
        ["document.generateLink"] = "Generer dokumentlenke",
        ["document.deletePermanently"] = "Slett permanent",
        ["translation.newTranslation"] = "Ny oversettelse",
        ["translation.yesDelete"] = "Ja, slett"
    };
}
