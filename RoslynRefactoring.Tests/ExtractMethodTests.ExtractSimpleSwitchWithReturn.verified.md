## Original

```csharp

public class Bird
{
    private int kind;

    public int GetSpeed()
    {
        switch (kind)
        {
            case 0: return 10;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}
```

## Selected Span

```csharp
        switch (kind)
        {
            case 0: return 10;
            default: throw new ArgumentOutOfRangeException();
        }

```

---

## Refactored

```csharp
public class Bird
{
    private int kind;
    public int GetSpeed()
    {
        return ComputeSpeed();
    }

    private int ComputeSpeed()
    {
        switch (kind)
        {
            case 0:
                return 10;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
```