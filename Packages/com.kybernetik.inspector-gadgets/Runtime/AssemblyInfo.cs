// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2023 Kybernetik //

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Kybernetik.InspectorGadgets")]
[assembly: AssemblyDescription("A variety of tools which enhance the Unity Editor.")]
[assembly: AssemblyCompany("Kybernetik")]
[assembly: AssemblyProduct("Inspector Gadgets Lite")]
[assembly: AssemblyCopyright("Copyright © Kybernetik 2017-2023")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion(InspectorGadgets.Strings.InspectorGadgetsVersion + ".0.0")]

[assembly: InternalsVisibleTo("Kybernetik.InspectorGadgets.Editor")]

#if UNITY_EDITOR

[assembly: SuppressMessage("Style", "IDE0016:Use 'throw' expression",
    Justification = "Not supported by older Unity versions.")]
[assembly: SuppressMessage("Style", "IDE0019:Use pattern matching",
    Justification = "Not supported by older Unity versions.")]
[assembly: SuppressMessage("Style", "IDE0039:Use local function",
    Justification = "Not supported by older Unity versions.")]
[assembly: SuppressMessage("Style", "IDE0044:Make field readonly",
    Justification = "Using the [SerializeField] attribute on a private field means Unity will set it from serialized data.")]
[assembly: SuppressMessage("Code Quality", "IDE0051:Remove unused private members",
    Justification = "Unity messages can be private, but the IDE will not know that Unity can still call them.")]
[assembly: SuppressMessage("Code Quality", "IDE0052:Remove unread private members",
    Justification = "Unity messages can be private and don't need to be called manually.")]
[assembly: SuppressMessage("Style", "IDE0059:Value assigned to symbol is never used",
    Justification = "Inline variable declarations are not supported by older Unity versions.")]
[assembly: SuppressMessage("Style", "IDE0060:Remove unused parameter",
    Justification = "Unity messages sometimes need specific signatures, even if you don't use all the parameters.")]
[assembly: SuppressMessage("Style", "IDE0063:Use simple 'using' statement",
    Justification = "Not supported by older Unity versions.")]
[assembly: SuppressMessage("Style", "IDE0066:Convert switch statement to expression",
    Justification = "Not supported by older Unity versions.")]
[assembly: SuppressMessage("Code Quality", "IDE0067:Dispose objects before losing scope",
    Justification = "Not always relevant.")]
[assembly: SuppressMessage("Code Quality", "IDE0068:Use recommended dispose pattern",
    Justification = "Not always relevant.")]
[assembly: SuppressMessage("Code Quality", "IDE0069:Disposable fields should be disposed",
    Justification = "Not always relevant.")]
[assembly: SuppressMessage("Style", "IDE0074:Use compound assignment",
    Justification = "Not supported by older Unity versions.")]
[assembly: SuppressMessage("Style", "IDE0083:Use pattern matching",
    Justification = "Not supported by older Unity versions")]
[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'",
    Justification = "Not supported by older Unity versions.")]
[assembly: SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression",
    Justification = "Don't give code style advice in publically released code.")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles",
    Justification = "Don't give code style advice in publically released code.")]

[assembly: SuppressMessage("Type Safety", "UNT0014:Invalid type for call to GetComponent",
    Justification = "Doesn't account for generic constraints.")]

[assembly: SuppressMessage("Code Quality", "CS0649:Field is never assigned to, and will always have its default value",
    Justification = "Using the [SerializeField] attribute on a private field means Unity will set it from serialized data.")]

#endif
