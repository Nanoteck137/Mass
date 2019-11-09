; ModuleID = 'test01.c'
source_filename = "test01.c"
target datalayout = "e-m:e-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

%struct.T = type { %struct.R, i32, i32 }
%struct.R = type { i32, i32 }

@w = global %struct.T { %struct.R { i32 1, i32 2 }, i32 2, i32 3 }, align 4
@test.t = private unnamed_addr constant %struct.R { i32 1, i32 2 }, align 4
@main.e = private unnamed_addr constant %struct.T { %struct.R { i32 1, i32 2 }, i32 2, i32 3 }, align 4

; Function Attrs: noinline nounwind optnone uwtable
define i64 @test() #0 {
  %1 = alloca %struct.R, align 4
  %2 = alloca %struct.R, align 4
  %3 = bitcast %struct.R* %2 to i8*
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* %3, i8* bitcast (%struct.R* @test.t to i8*), i64 8, i32 4, i1 false)
  %4 = bitcast %struct.R* %1 to i8*
  %5 = bitcast %struct.R* %2 to i8*
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* %4, i8* %5, i64 8, i32 4, i1 false)
  %6 = bitcast %struct.R* %1 to i64*
  %7 = load i64, i64* %6, align 4
  ret i64 %7
}

; Function Attrs: argmemonly nounwind
declare void @llvm.memcpy.p0i8.p0i8.i64(i8* nocapture writeonly, i8* nocapture readonly, i64, i32, i1) #1

; Function Attrs: noinline nounwind optnone uwtable
define i32 @main(i32, i8**) #0 {
  %3 = alloca i32, align 4
  %4 = alloca i32, align 4
  %5 = alloca i8**, align 8
  %6 = alloca %struct.T, align 4
  store i32 0, i32* %3, align 4
  store i32 %0, i32* %4, align 4
  store i8** %1, i8*** %5, align 8
  %7 = bitcast %struct.T* %6 to i8*
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* %7, i8* bitcast (%struct.T* @main.e to i8*), i64 16, i32 4, i1 false)
  ret i32 0
}

attributes #0 = { noinline nounwind optnone uwtable "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="true" "no-frame-pointer-elim-non-leaf" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+fxsr,+mmx,+sse,+sse2,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { argmemonly nounwind }

!llvm.module.flags = !{!0}
!llvm.ident = !{!1}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{!"clang version 6.0.0-1ubuntu2 (tags/RELEASE_600/final)"}
