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
	var x: u32 = 3;
	var y: u32 = 4;

	if(x == 3) {
		if(y == 4) {
			printf("Y is equal to 4\n");
		}
		printf("X is equal to 3\n");

		x = x - 1;
	}

	if(x == 2) {
		printf("x is equal to 2\n");
	}

	// printf("Value: %d\n", y);

	ret 0;
}
