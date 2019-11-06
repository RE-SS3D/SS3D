# The C# Style Guide

This style guide is based on [this other C# style guide](https://github.com/raywenderlich/c-sharp-style-guide/blob/master/README.markdown)
and expands upon it.

Our overarching goals are **clarity**, **readability** and **simplicity**. Also, this guide is written to keep **Unity** in mind. 

## Inspiration

This style guide is based on C# and Unity conventions, and discussions within the development team.

## Table of Contents

- [Nomenclature](#nomenclature)
  + [Namespaces](#namespaces)
  + [Classes & Interfaces](#classes--interfaces)
  + [Methods](#methods)
  + [Fields](#fields)
  + [Parameters](#parameters--parameters)
  + [Delegates](#delegates--delegates)
  + [Events](#events--events)
  + [Misc](#misc)
- [Declarations](#declarations)
  + [Access Level Modifiers](#access-level-modifiers)
  + [Fields & Variables](#fields--variables)
  + [Classes](#classes)
  + [Interfaces](#interfaces)
- [Spacing](#spacing)
  + [Indentation](#indentation)
  + [Line Length](#line-length)
  + [Vertical Spacing](#vertical-spacing)
- [Brace Style](#brace-style)
- [Switch Statements](#switch-statements)
- [Language](#language)
- [Common Patterns and Structure](#common-patterns-and-structure)


## Nomenclature

On the whole, naming should follow C# standards.

### Namespaces

Namespaces are all **PascalCase**, multiple words concatenated together, without hypens ( - ) or underscores ( \_ ):

Namespaces should be used for major systems, such as Atmospherics, or Electrics. Everything else should be without namespace.

**BAD**:

```csharp
com.ress3dclient.scripts.structures
```

**GOOD**:

```csharp
Structures.Door
```

### Classes & Interfaces

Written in **PascalCase**. For example `RadialSlider`. 

### Methods

Methods are written in **PascalCase**. For example `DoSomething()`. 

### Fields

All non-static fields are written **camelCase**. Per Unity convention, this includes **public fields** as well.

For example:

```csharp
public class MyClass 
{
    public int publicField;
    private int packagePrivate;
    private int myPrivate;
    protected int myProtected;
}
```

**BAD:**

```csharp
private int _myPrivateVariable
```

**GOOD:**

```csharp
private int myPrivateVariable
```

Static fields are the exception and should be written in **PascalCase**:

```csharp
public static int TheAnswer = 42;
```

### Parameters

Parameters are written in **camelCase**.

**BAD:**

```csharp
void DoSomething(Vector3 Location)
```
**GOOD:**

```csharp
void DoSomething(Vector3 location)
```

Single character values are to be avoided except for temporary looping variables.

### Delegates

Delegates are written in **PascalCase**.

When declaring delegates, DO add the suffix **EventHandler** to names of delegates that are used in events. 

**BAD:**

```csharp
public delegate void Click()
```
**GOOD:**

```csharp
public delegate void ClickEventHandler()
```  

DO add the suffix **Callback** to names of delegates other than those used as event handlers.

**BAD:**

```csharp
public delegate void Render()
```
**GOOD:**

```csharp
public delegate void RenderCallback()
```  

Using built.in C# features, such as Action<T>, is encouraged in place of delegates.

### Events

Prefix event methods with the prefix **On**.

**BAD:**

```csharp
public static event CloseCallback Close;
```  

**GOOD:**

```csharp
public static event CloseCallback OnClose;
```

### Misc

In code, acronyms should be treated as words. For example:

**BAD:**

```csharp
XMLHTTPRequest
String URL
findPostByID
```  

**GOOD:**

```csharp
XmlHttpRequest
String url
findPostById
```

## Declarations

### Access Level Modifiers

Access level modifiers should be explicitly defined for classes, methods and member variables. 
This includes defining private even though C# will implicitly add it.

Use the least accessible access modifier, except for public member that is expected to be used by other classes in the future.

### Fields & Variables

Prefer single declaration per line.

**BAD:**

```csharp
string username, twitterHandle;
```

**GOOD:**

```csharp
string username;
string twitterHandle;
```

### Classes

Exactly one class, struct, or interface per source file, although inner classes are encouraged where scoping appropriate.

### Interfaces

All interfaces should be prefaced with the letter **I**. 

**BAD:**

```csharp
RadialSlider
```

**GOOD:**

```csharp
IRadialSlider
```

## Spacing

Spacing is especially important to make code more readable. 

### Indentation

Indentation should be done using **spaces** — never tabs.  

#### Blocks

Indentation for blocks uses **4 spaces** for optimal readability:

**BAD:**

```csharp
for (int i = 0; i < 10; i++) 
{
  Debug.Log("index=" + i);
}
```

**GOOD:**

```csharp
for (int i = 0; i < 10; i++) 
{
    Debug.Log("index=" + i);
}
```

#### Line Wraps

Indentation for line wraps should use **4 spaces** (not the default 8):

**BAD:**

```csharp
CoolUiWidget widget =
        someIncrediblyLongExpression(that, reallyWouldNotFit, on, aSingle, line);
```

**GOOD:**

```csharp
CoolUiWidget widget =
    someIncrediblyLongExpression(that, reallyWouldNotFit, on, aSingle, line);
```

### Line Length

Lines should be no longer than **100** characters long.

### Vertical Spacing

There should be just one or two blank lines between methods to aid in visual clarity 
and organization. Whitespace within methods should separate functionality, but 
having too many sections in a method often means you should refactor into
several methods.


## Brace Style

All braces get their own line as it is a C# convention:

**BAD:**

```csharp
class MyClass {
    void DoSomething() {
        if (someTest) {
          // ...
        } else {
          // ...
        }
    }
}
```

**GOOD:**

```csharp
class MyClass
{
    void DoSomething()
    {
        if (someTest)
        {
          // ...
        }
        else
        {
          // ...
        }
    }
}
```

Conditional statements are always required to be enclosed with braces,
irrespective of the number of lines required.

**BAD:**

```csharp
if (someTest)
    doSomething();  

if (someTest) doSomethingElse();
```

**GOOD:**

```csharp
if (someTest) 
{
    DoSomething();
}  

if (someTest)
{
    DoSomethingElse();
}
```
## Switch Statements

Switch-statements come with `default` case by default (heh). If the `default` case is never reached, be sure to remove it.

If the `default` case is an unexpected value, it is encouraged to log and return an error

**BAD**  
  
```csharp
switch (variable) 
{
    case 1:
        break;
    case 2:
        break;
    default:
        break;
}
```

**GOOD**  
  
```csharp
switch (variable) 
{
    case 1:
        break;
    case 2:
        break;
}
```

**BETTER**  
  
```csharp
switch (variable) 
{
    case 1:
        break;
    case 2:
        break;
    default:
        Debug.LogError("Unexpected value when...");
        return;
}
```

## Language

Use US English spelling.

**BAD:**

```csharp
string colour = "red";
```

**GOOD:**

```csharp
string color = "red";
```

The exception here is `MonoBehaviour` as that's what the class is actually called.

## Common Patterns and Structure

This section includes some rules of thumb for design patterns and code structure

### Error handling

Avoid throwing exceptions. Instead log and error. Methods returning values should return null in addition to logging an error

**BAD:**
```csharp
public List<Transform> FindAThing(int arg){
    ...
    if (notFound) {
        throw new NotFoundException();
    }
}
```

**GOOD:**
```csharp
public List<Transform> FindAThing(int arg){
    ...
    if (notFound) {
        Debug.LogError("Thing not found");
        return null;
    }
}
```

### Default Script Comments

We know what Start and Update does. Please remove their comments when you create them through the editor.

***NO:***
```csharp
// Use this for initialization
void Start() {
    ...
}
```

**YES:**
```csharp
private void Start() {
    ...
}
```

### Finding references

Don't use Find or in other ways refer to game objects by name or child index *when possible*. Reference by reference is less error prone and more flexible. 
Expect people to set the fields in the inspector and log warnings if they don't.

**BAD:**
```csharp
private GameObject someMember;

private void Start() {
    someMember = GameObject.Find("ObjectName");
}
```

**GOOD:**
```csharp
public GameObject someMember;
```

### RequireComponent

Prefer RequireComponent and GetComponent over AddComponent. Having the components in the inspector let's us edit them. AddComponent limits us.

**BAD:**
```csharp
public class : MonoBehaviour
{
    private AudioSource audioSource;

    private void Start() {
        audioSource = gameObject.AddCompenent<AudioSource>();
    }
}
```

**GOOD:**
```csharp
[RequireComponent(typeof(AudioSource))]
public class : MonoBehaviour
{
    private AudioSource audioSource;

    private void Start() {
        audioSource = GetCompenent<AudioSource>();
    }
}
```

### Properties with backing fields

Properties can be used for access control to fields, and when using backing fields they can be private and let us change them in the inspector. Consider when a fields should be public and prefer properties with backing fields.

Sometimes it's just nice to see them for debugging, even if we don't change them, so consider making more of your private fields visible.

**OKAY:**
```csharp
public GameObject someMember;
```

**BETTER:**
```csharp
[SerializeField] private GameObject someMember;
public GameObject SomeMember => someMember;
```
