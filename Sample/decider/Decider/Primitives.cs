namespace Decider;

public class Maybe<TSomething>
{
    public bool IsPresent { get; }

    private readonly TSomething? value;

    private Maybe(TSomething value, bool isPresent)
    {
        this.value = value;
        this.IsPresent = isPresent;
    }

    public static readonly Maybe<TSomething> Empty = new(default!, false);

    public static Maybe<TSomething> Of(TSomething value) => value != null ? new Maybe<TSomething>(value, true) : Empty;

    public TSomething GetOrThrow() =>
        IsPresent ? value! : throw new ArgumentNullException(nameof(value));

    public TSomething GetOrDefault(TSomething defaultValue = default!) =>
        IsPresent ? value ?? defaultValue : defaultValue;
}

public class Either<TLeft, TRight>
{
    public Maybe<TLeft> Left { get; }
    public Maybe<TRight> Right { get; }

    public Either(TLeft value)
    {
        Left = Maybe<TLeft>.Of(value);
        Right = Maybe<TRight>.Empty;
    }

    public Either(TRight value)
    {
        Left = Maybe<TLeft>.Empty;
        Right = Maybe<TRight>.Of(value);
    }

    public Either(Maybe<TLeft> left, Maybe<TRight> right)
    {
        if (!left.IsPresent && !right.IsPresent)
            throw new ArgumentOutOfRangeException(nameof(right));

        Left = left;
        Right = right;
    }

    public TMapped Map<TMapped>(
        Func<TLeft, TMapped> mapLeft,
        Func<TRight, TMapped> mapRight
    )
    {
        if (Left.IsPresent)
            return mapLeft(Left.GetOrThrow());

        if (Right.IsPresent)
            return mapRight(Right.GetOrThrow());

        throw new Exception("That should never happen!");
    }

    public void Switch(
        Action<TLeft> onLeft,
        Action<TRight> onRight
    )
    {
        if (Left.IsPresent)
        {
            onLeft(Left.GetOrThrow());
            return;
        }

        if (Right.IsPresent)
        {
            onRight(Right.GetOrThrow());
            return;
        }

        throw new Exception("That should never happen!");
    }
}

public class OneOf<T1, T2, T3>
{
    public Maybe<T1> First { get; }
    public Maybe<T2> Second { get; }
    public Maybe<T3> Third { get; }

    public OneOf(T1 value)
    {
        First = Maybe<T1>.Of(value);
        Second = Maybe<T2>.Empty;
        Third = Maybe<T3>.Empty;
    }

    public OneOf(T2 value)
    {
        First = Maybe<T1>.Empty;
        Second = Maybe<T2>.Of(value);
        Third = Maybe<T3>.Empty;
    }

    public OneOf(T3 value)
    {
        First = Maybe<T1>.Empty;
        Second = Maybe<T2>.Empty;
        Third = Maybe<T3>.Of(value);
    }

    public OneOf((T1? First, T2? Second, T3? Third) value)
    {
        First = value.First != null ? Maybe<T1>.Of(value.First) : Maybe<T1>.Empty;
        Second = value.Second != null ? Maybe<T2>.Of(value.Second) : Maybe<T2>.Empty;
        Third = value.Third != null ? Maybe<T3>.Of(value.Third) : Maybe<T3>.Empty;
    }

    public TMapped Map<TMapped>(
        Func<T1, TMapped> mapT1,
        Func<T2, TMapped> mapT2,
        Func<T3, TMapped> mapT3
    )
    {
        if (First.IsPresent)
            return mapT1(First.GetOrThrow());

        if (Second.IsPresent)
            return mapT2(Second.GetOrThrow());

        if (Third.IsPresent)
            return mapT3(Third.GetOrThrow());

        throw new Exception("That should never happen!");
    }

    public void Switch(
        Action<T1> onT1,
        Action<T2> onT2,
        Action<T3> onT3
    )
    {
        if (First.IsPresent)
        {
            onT1(First.GetOrThrow());
            return;
        }

        if (Second.IsPresent)
        {
            onT2(Second.GetOrThrow());
            return;
        }

        if (Third.IsPresent)
        {
            onT3(Third.GetOrThrow());
            return;
        }

        throw new Exception("That should never happen!");
    }
}

public static class EitherExtensions
{
    public static (TLeft? Left, TRight? Right) AssertAnyDefined<TLeft, TRight>(
        this (TLeft? Left, TRight? Right) value
    )
    {
        if (value.Left == null && value.Right == null)
            throw new ArgumentOutOfRangeException(nameof(value), "One of values needs to be set");

        return value;
    }

    public static TMapped Map<TLeft, TRight, TMapped>(
        this (TLeft? Left, TRight? Right) value,
        Func<TLeft, TMapped> mapLeft,
        Func<TRight, TMapped> mapRight
    )
        where TLeft: struct
        where TRight: struct
    {
        var (left, right) = value.AssertAnyDefined();

        if (left.HasValue)
            return mapLeft(left.Value);

        if (right.HasValue)
            return mapRight(right.Value);

        throw new Exception("That should never happen!");
    }

    public static TMapped Map<TLeft, TRight, TMapped>(
        this (TLeft? Left, TRight? Right) value,
        Func<TLeft, TMapped> mapT1,
        Func<TRight, TMapped> mapT2
    )
    {
        value.AssertAnyDefined();

        var either = value.Left != null
            ? new Either<TLeft, TRight>(value.Left!)
            : new Either<TLeft, TRight>(value.Right!);

        return either.Map(mapT1, mapT2);
    }

    public static void Switch<TLeft, TRight>(
        this (TLeft? Left, TRight? Right) value,
        Action<TLeft> onT1,
        Action<TRight> onT2
    )
    {
        value.AssertAnyDefined();

        var either = value.Left != null
            ? new Either<TLeft, TRight>(value.Left!)
            : new Either<TLeft, TRight>(value.Right!);

        either.Switch(onT1, onT2);
    }

    public static (TLeft?, TRight?) Either<TLeft, TRight>(
        TLeft? left = default
    ) => (left, default);

    public static (TLeft?, TRight?) Either<TLeft, TRight>(
        TRight? right = default
    ) => (default, right);
}

public static class OneOfExtensions
{
    public static void Map<T1, T2, T3, TMapped>(
        this (T1? First, T2? Second, T3? Third) value,
        Func<T1, TMapped> mapT1,
        Func<T2, TMapped> mapT2,
        Func<T3, TMapped> mapT3
    ) => new OneOf<T1, T2, T3>(value).Map(mapT1, mapT2, mapT3);

    public static void Switch<T1, T2, T3, TMapped>(
        this (T1? First, T2? Second, T3? Third) value,
        Action<T1> onT1,
        Action<T2> onT2,
        Action<T3> onT3
    ) => new OneOf<T1, T2, T3>(value).Switch(onT1, onT2, onT3);
}
