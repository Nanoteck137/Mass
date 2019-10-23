; ModuleID = 'test.ma'
source_filename = "test.ma"

@0 = private unnamed_addr constant [13 x i8] c"Hello World\0A\00", align 1
@1 = private unnamed_addr constant [14 x i8] c"%d + %d = %d\0A\00", align 1

declare i32 @printf(i8*, ...)

define void @test() {
entry:
  %0 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([13 x i8], [13 x i8]* @0, i32 0, i32 0))
  ret void
}

define i32 @add(i32 %a, i32 %b) {
entry:
  %s_a = alloca i32
  store i32 %a, i32* %s_a
  %s_b = alloca i32
  store i32 %b, i32* %s_b
  %sum = alloca i32
  %0 = load i32, i32* %s_a
  %1 = load i32, i32* %s_b
  %addRes = add i32 %0, %1
  store i32 %addRes, i32* %sum
  call void @test()
  %2 = load i32, i32* %s_a
  %3 = load i32, i32* %s_b
  %4 = load i32, i32* %sum
  %5 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([14 x i8], [14 x i8]* @1, i32 0, i32 0), i32 %2, i32 %3, i32 %4)
  %6 = load i32, i32* %sum
  ret i32 %6
}

define i32 @main(i32 %argc, i8** %argv) {
entry:
  %s_argc = alloca i32
  store i32 %argc, i32* %s_argc
  %s_argv = alloca i8**
  store i8** %argv, i8*** %s_argv
  %0 = call i32 @add(i32 2, i32 3)
  ret i32 0
}
