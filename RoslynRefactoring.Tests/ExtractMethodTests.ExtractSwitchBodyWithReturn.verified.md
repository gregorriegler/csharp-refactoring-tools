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
eturn 10;

```

---

## Refactored

```csharp
public class Bird
{
    private int kind;
    public int GetSpeed()
    {
        switch (kind)
        {
            case 0:
                return Ten();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private int Ten()
    {
        return 10;
    }
}
```