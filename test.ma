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
	var a: u32[4] = { 1, 2, 3, 4 };
	var ptr: u32* = a as u32*;
	var offset: u64 = 3;

	var testPtr: u32* = ptr + offset;

	printf("Value: %u\n", deref(testPtr - 2));
	ret 0;
}
