// Hello World
const B: i32 = A;
const A: i32 = 1;

//external func printf(format: u8*, ...) -> i32;

func add(a: i32, b: i32) -> i32
{
    var sum: int = a + b;
    printf("%d + %d = %d", a, b, sum);

    ret sum;
}

// Wooh