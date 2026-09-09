global using Xunit;

// WPF allows exactly one Application per process, and a Window belongs to the
// thread that created it. Running test classes in parallel would race both.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
