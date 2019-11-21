import libc;
import printf from libc;

func add(a: s32, b: s32) -> s32 {
    ret a + b;
}

func main(argc: s32, argv: u8**) -> s32 {
    libc.printf("Result: %d\n", add(4, 8));
    printf("Result: %d\n", add(4, 8));
    ret 0;
}