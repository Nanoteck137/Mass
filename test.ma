// Hello World
// const B: i32 = A;
// const A: i32 = 1 + 2;

external func printf(format: u8*, ...) -> s32;

func test() {
	printf("Hello World\n");
}

func add(a: s32, b: s32) -> s32
{
    var sum: s32 = a + b;
	test();
    printf("%d + %d = %d\n", a, b, sum);

    ret sum;
}

func main(argc: s32, argv: u8**) -> s32 {
	add(2, 3);
	ret 0;
}

// Wooh