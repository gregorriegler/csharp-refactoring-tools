## Original

```csharp

public class Console
{
    public void Write()
    {
        Console.WriteLine("Hello World");
    }
}
```

## Selected Span

```csharp
        Console.WriteLine("Hello World");
  
```

---

## Refactored

```csharp
public class Console
{
    public void Write()
    {
        Write();
    }

    private void Write()
    {
        Console.WriteLine("Hello World");
    }
}
```