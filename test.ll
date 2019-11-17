; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [9 x i8] c"Val: %u\0A\00", align 1

declare i32 @printf(i8*, ...)

declare %struct.FILE* @fopen(i8*, i8*)

declare i64 @fwrite(i8*, i64, i64, %struct.FILE*)

declare i32 @fclose(%struct.FILE*)

declare i64 @strlen(i8*)

declare i8* @malloc(i64)

declare void @free(i8*)

define i32 @main(i32 %argc, i8** %argv) {
entry:
  %argc1 = alloca i32
  store i32 %argc, i32* %argc1
  %argv2 = alloca i8**
  store i8** %argv, i8*** %argv2
  %ptr = alloca i32*
  %0 = call i8* @malloc(i64 8)
  %1 = bitcast i8* %0 to i32*
  store i32* %1, i32** %ptr
  %2 = load i32*, i32** %ptr
  %3 = getelementptr i32, i32* %2, i32 0
  store i32 123, i32* %3
  %4 = load i32*, i32** %ptr
  %5 = getelementptr i32, i32* %4, i32 1
  store i32 321, i32* %5
  %6 = load i32*, i32** %ptr
  %7 = getelementptr i32, i32* %6, i32 1
  %8 = load i32, i32* %7
  %9 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @str, i32 0, i32 0), i32 %8)
  %10 = load i32*, i32** %ptr
  %11 = bitcast i32* %10 to i8*
  call void @free(i8* %11)
  ret i32 0
}
