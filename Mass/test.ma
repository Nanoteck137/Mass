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

#external
func malloc(size: u64) -> u8*;

#external
func free(ptr: u8*);

func main(argc: s32, argv: u8**) -> s32
{
	var a: s32 = 3;

	if(a < 5) 
	{
		printf("1\n");
	} 
	else if(a < 8)
	{
		printf("2\n");
	} 
	else if(a < 10)
	{
		printf("3\n");
	}

	ret 0;
}
