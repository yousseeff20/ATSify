namespace ATS.Domain.Constants;

public static class Permissions
{
    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string Create = "Permissions.Users.Create";
        public const string Update = "Permissions.Users.Update";
        public const string Delete = "Permissions.Users.Delete";
    }

    public static class Roles
    {
        public const string View = "Permissions.Roles.View";
        public const string Create = "Permissions.Roles.Create";
        public const string Update = "Permissions.Roles.Update";
        public const string Delete = "Permissions.Roles.Delete";
    }

    public static class Companies
    {
        public const string View = "Permissions.Companies.View";
        public const string Create = "Permissions.Companies.Create";
        public const string Update = "Permissions.Companies.Update";
        public const string Delete = "Permissions.Companies.Delete";
    }

    public static class Departments
    {
        public const string View = "Permissions.Departments.View";
        public const string Create = "Permissions.Departments.Create";
        public const string Update = "Permissions.Departments.Update";
        public const string Delete = "Permissions.Departments.Delete";
    }

    public static class Invitations
    {
        public const string View = "Permissions.Invitations.View";
        public const string Send = "Permissions.Invitations.Send";
        public const string Resend = "Permissions.Invitations.Resend";
        public const string Cancel = "Permissions.Invitations.Cancel";
    }

    public static class Jobs
    {
        public const string View = "Permissions.Jobs.View";
        public const string Create = "Permissions.Jobs.Create";
        public const string Update = "Permissions.Jobs.Update";
        public const string Publish = "Permissions.Jobs.Publish";
        public const string Close = "Permissions.Jobs.Close";
        public const string Archive = "Permissions.Jobs.Archive";
    }

    public static IReadOnlyList<string> GetAll()
    {
        return new[]
        {
            Users.View, Users.Create, Users.Update, Users.Delete,
            Roles.View, Roles.Create, Roles.Update, Roles.Delete,
            Companies.View, Companies.Create, Companies.Update, Companies.Delete,
            Departments.View, Departments.Create, Departments.Update, Departments.Delete,
            Invitations.View, Invitations.Send, Invitations.Resend, Invitations.Cancel,
            Jobs.View, Jobs.Create, Jobs.Update, Jobs.Publish, Jobs.Close, Jobs.Archive
        };
    }
}
