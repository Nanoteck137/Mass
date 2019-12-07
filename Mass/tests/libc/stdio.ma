#export
struct TestStruct {
	a: s32;
	b: s32;
}

#external
#export
func printf(format: u8*, ...);

#export
func add(a: s32, b: s32) -> s32 {
	ret a + b;
}