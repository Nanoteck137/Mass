#external
struct FILE;

#external
func printf(format: u8*, ...) -> s32;

#external
func fopen(filename: u8*, mode: u8*) -> FILE*;

#external
func fwrite(ptr: u8*, size: u64, count: u64, file: FILE*) -> u64;

#external
func fclose(file: FILE*) -> s32;

#external
func strlen(str: u8*) -> u64;

func main(argc: s32, argv: u8**) -> s32 
{
	var file: FILE* = fopen("test.txt", "wt");
	printf("File Ptr: %p\n", file);

	var content: u8* = "Test Str Content";
	fwrite(content, strlen(content), 1, file);

	fclose(file);

	var ta: s32 = 3 + 4 * 2;
	var ptr: s32* = addr(ta);
	printf("ta Value: %d Address: %p Deref: %d\n", ta, ptr, deref(ptr));
	printf("Test: %s\n", deref(argv));

	ret 0;
}

// Wooh