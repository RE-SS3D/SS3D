namespace SS3D.Systems.IdAccess
{
    public static class DepartmentDisplay
    {
        public static string GetName(Department department)
        {
            return department switch
            {
                Department.Command => "Command",
                Department.Security => "Security",
                Department.Engineering => "Engineering",
                Department.Medical => "Medical",
                Department.Science => "Science",
                Department.Cargo => "Cargo",
                Department.Service => "Service",
                Department.Civilian => "Civilian",
                _ => string.Empty,
            };
        }
    }
}
