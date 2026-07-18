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

    public static IReadOnlyList<string> GetAll()
    {
        return new[]
        {
            Users.View, Users.Create, Users.Update, Users.Delete,
            Roles.View, Roles.Create, Roles.Update, Roles.Delete
        };
    }
}
