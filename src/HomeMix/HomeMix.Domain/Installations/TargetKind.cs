namespace HomeMix.Domain.Installations;

/// <summary>Where an add-on is installed: a household (role Admin and up installs it) or one person's account.</summary>
public enum TargetKind
{
    Household = 0,
    User = 1,
}
