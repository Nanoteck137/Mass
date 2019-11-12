#external
struct FILE;

#external
func printf(format: u8*, ...) -> s32;

#external
func fopen(filename: u8*, mode: u8*) -> FILE*;

#external
func fclose(file: FILE*) -> s32;

var ta: s32[2][2] = { {1,2}, {3, 4} };

func main(argc: s32, argv: u8**) -> s32 
{
	// add(2, 3);

	var file: FILE* = fopen("test.txt", "wt");

	fclose(file);

	// ta = t.a;
	// t.a[0] = 123;
	printf("Array: %d, %d, %d, %d\n", ta[0], ta[1], ta[2], ta[3]);

	printf("Value: %d\n", ta[1][0]);

	var ta: s32 = 3 + 4 * 2;

	var ptr: s32* = addr(ta);
	printf("ta Value: %d Address: %p Deref: %d\n", ta, ptr, deref(ptr));
	printf("Test: %s\n", deref(argv));

	ret 0;
}

// Wooh