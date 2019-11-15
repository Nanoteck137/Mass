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
	var x: u32 = 4;
	var y: u32 = x++;

	printf("X: %d\n", x);
	printf("Y: %d\n", y--);
	printf("Y: %d\n", y);

	ret 0;
}
