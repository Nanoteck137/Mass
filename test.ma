// Hello World
// const B: i32 = A;
// const A: i32 = 1 + 2;

var a: s32 = 123;

#external
func printf(format: u8*, ...) -> s32;

func add(a: s32, b: s32) -> s32
{
    var sum: s32 = a + b;

    printf("%d + %d = %d\n", a, b, sum);

    ret sum;
}

struct TestStruct
{
	a: s32[4];
}

// var t: TestStruct = { { 4, 3, 2, 1 } };
// var ta: s32[4] = { 1, 2, 3, 4 };

func main(argc: s32, argv: u8**) -> s32 
{
	// add(2, 3);

	// ta = t.a;
	// t.a[0] = 123;

	var ta: s32 = 3 + 4 * 2;
	// printf("Array: %d, %d, %d, %d", ta[0], ta[1], ta[2], ta[3]);
	printf("Value of ta: %d\n", ta);

	ret 0;
}

// Wooh