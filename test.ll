; ModuleID = 'NO NAME'
source_filename = "NO NAME"

@ta = global [2 x [2 x i32]] [[2 x i32] [i32 1, i32 2], [2 x i32] [i32 3, i32 4]]
@str = private unnamed_addr constant [23 x i8] c"Array: %d, %d, %d, %d\0A\00", align 1
@str.1 = private unnamed_addr constant [11 x i8] c"Value: %d\0A\00", align 1
@str.2 = private unnamed_addr constant [36 x i8] c"ta Value: %d Address: %p Deref: %d\0A\00", align 1
@str.3 = private unnamed_addr constant [10 x i8] c"Test: %c\0A\00", align 1

declare i32 @printf(i8*, ...)

define i32 @main(i32 %argc, i8** %argv) {
  %argc1 = alloca i32
  store i32 %argc, i32* %argc1
  %argv2 = alloca i8**
  store i8** %argv, i8*** %argv2
  %1 = load [2 x i32], [2 x i32]* getelementptr inbounds ([2 x [2 x i32]], [2 x [2 x i32]]* @ta, i32 0, i32 0)
  %2 = load [2 x i32], [2 x i32]* getelementptr inbounds ([2 x [2 x i32]], [2 x [2 x i32]]* @ta, i32 0, i32 1)
  %3 = load [2 x i32], [2 x i32]* getelementptr inbounds ([2 x [2 x i32]], [2 x [2 x i32]]* @ta, i64 1, i32 0)
  %4 = load [2 x i32], [2 x i32]* getelementptr ([2 x [2 x i32]], [2 x [2 x i32]]* @ta, i64 1, i32 1)
  %5 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([23 x i8], [23 x i8]* @str, i32 0, i32 0), [2 x i32] %1, [2 x i32] %2, [2 x i32] %3, [2 x i32] %4)
  %6 = load [2 x i32], [2 x i32]* getelementptr inbounds ([2 x [2 x i32]], [2 x [2 x i32]]* @ta, i32 0, i32 1)
  %7 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([11 x i8], [11 x i8]* @str.1, i32 0, i32 0), [2 x i32] %6)
  %ta = alloca i32
  store i32 11, i32* %ta
  %ptr = alloca i32*
  store i32* %ta, i32** %ptr
  %8 = load i32, i32* %ta
  %9 = load i32*, i32** %ptr
  %10 = load i32*, i32** %ptr
  %11 = load i32, i32* %10
  %12 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([36 x i8], [36 x i8]* @str.2, i32 0, i32 0), i32 %8, i32* %9, i32 %11)
  %13 = load i8**, i8*** %argv2
  %14 = getelementptr i8*, i8** %13, i32 0
  %15 = load i8*, i8** %14
  %16 = getelementptr i8, i8* %15, i32 0
  %17 = load i8, i8* %16
  %18 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([10 x i8], [10 x i8]* @str.3, i32 0, i32 0), i8 %17)
  ret i32 0
}
