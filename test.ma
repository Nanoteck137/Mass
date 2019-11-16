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
	var a: s32 = 4;
	var b: s32 = 5;

	if(a == 4 || b == 4) {
		printf("Equal\n");
	}

	ret 0;
}
