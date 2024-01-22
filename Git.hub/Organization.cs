namespace Git.hub;

public class Organization
{
    public string Login { get; internal set; }

    public override bool Equals(object obj) => GetHashCode() == obj.GetHashCode();
    public override int GetHashCode() => GetType().GetHashCode() + Login.GetHashCode();
    public override string ToString() => Login;
}
