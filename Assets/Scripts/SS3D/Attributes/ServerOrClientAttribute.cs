using System;


/// <summary>
/// Informative only attribute, does not make any check.
/// Use it to let contributors know that method can be called client or server side.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class ServerOrClientAttribute : Attribute
{

}
