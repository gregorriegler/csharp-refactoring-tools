## Original

```csharp

public class UserProcessor
{
    public void ProcessUser(User user)
    {
        if (user.Age >= 18)
        {
            user.CanVote = true;
            user.Status = "Adult";
        }
        else
        {
            user.CanVote = false;
            user.Status = "Minor";
        }
    }
}

public class User
{
    public int Age { get; set; }
    public bool CanVote { get; set; }
    public string Status { get; set; }
}
```

## Selected Span

```csharp
        if (user.Age >= 18)
        {
            user.CanVote = true;
            user.Status = "Adult";
        }
        else
        {
            user.CanVote = false;
          
```

---

## Refactored

```csharp
public class UserProcessor
{
    public void ProcessUser(User user)
    {
        SetVotingStatus(user);
    }

    private void SetVotingStatus(User user)
    {
        if (user.Age >= 18)
        {
            user.CanVote = true;
            user.Status = "Adult";
        }
        else
        {
            user.CanVote = false;
            user.Status = "Minor";
        }
    }
}

public class User
{
    public int Age { get; set; }
    public bool CanVote { get; set; }
    public string Status { get; set; }
}
```