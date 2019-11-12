#external
func printf(format: u8*, ...) -> s32;

var ta: s32[2][2] = { {1,2}, {3, 4} };

func main(argc: s32, argv: u8**) -> s32 
{
	// add(2, 3);

	// ta = t.a;
	// t.a[0] = 123;
	printf("Array: %d, %d, %d, %d\n", ta[0], ta[1], ta[2], ta[3]);

	printf("Value: %d\n", ta[1][0]);

	var ta: s32 = 3 + 4 * 2;

	printf("ta Value: %d Address: %p\n", ta, addr(ta));
	printf("Test: %c\n", argv[0][0]);

	ret 0;
}

// Wooh